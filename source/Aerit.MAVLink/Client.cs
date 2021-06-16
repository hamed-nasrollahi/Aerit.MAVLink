#nullable enable

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	using Utils;
	using V2;

	public sealed partial class Client : ICommandClient, IDisposable
	{
		private readonly ITransmissionChannel transmissionChannel;
		private readonly byte systemId;
		private readonly byte componentId;

		private readonly CommandHandlerRegistry commandHandlers;

		public Client(ITransmissionChannel transmissionChannel, byte systemId, byte componentId)
		{
			this.transmissionChannel = transmissionChannel;
			this.systemId = systemId;
			this.componentId = componentId;

			commandHandlers = new(this);
		}

		private byte sequence = 0;

		private int FinalizeBuffer(byte[] buffer)
		{
			buffer[4] = sequence;

			var crc = Checksum.Seed;

			for (var i = 1; i <= (9 + buffer[1]); i++)
			{
				crc = Checksum.Compute(buffer[i], crc);
			}

			crc = Checksum.Compute(buffer[10 + buffer[1]], crc);

			buffer[10 + buffer[1]] = (byte)crc;
			buffer[11 + buffer[1]] = (byte)(crc >> 8);

			var length = 12 + buffer[1];

			if (((IncompatibilityFlags)buffer[2] & IncompatibilityFlags.Signed) != 0x00)
			{
				byte linkId = 0x00;
				buffer[12 + buffer[1]] = linkId;

				ulong timeStamp48 = 0;

				buffer[13 + buffer[1]] = (byte)timeStamp48;
				buffer[14 + buffer[1]] = (byte)(timeStamp48 >> 8);
				buffer[15 + buffer[1]] = (byte)(timeStamp48 >> 16);
				buffer[16 + buffer[1]] = (byte)(timeStamp48 >> 24);
				buffer[17 + buffer[1]] = (byte)(timeStamp48 >> 32);
				buffer[18 + buffer[1]] = (byte)(timeStamp48 >> 40);

				//var signature = Signature.Compute(key, buffer[..(12 + buffer[1] + 1 + 6)]);
				ulong signature = 0;

				buffer[19 + buffer[1]] = (byte)signature;
				buffer[20 + buffer[1]] = (byte)(signature >> 8);
				buffer[21 + buffer[1]] = (byte)(signature >> 16);
				buffer[22 + buffer[1]] = (byte)(signature >> 24);
				buffer[23 + buffer[1]] = (byte)(signature >> 32);
				buffer[24 + buffer[1]] = (byte)(signature >> 40);

				length += 13;
			}

			return length;
		}

		private async Task SendAsync(byte[] buffer)
		{
			try
			{
				var length = FinalizeBuffer(buffer);

				await transmissionChannel.SendAsync(buffer, length).ConfigureAwait(false);

				if (sequence == 255)
				{
					sequence = 0;
				}
				else
				{
					sequence++;
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		public CommandContext? Submit(CommandLong command)
		{
			var handler = commandHandlers.GetOrAdd(command.TargetSystem, command.TargetComponent, command.Command);

			if (!handler.TryAcquire())
			{
				return null;
			}

			return new CommandContext(handler, command);
		}

		public async Task ListenAsync(IBufferMiddleware pipeline, CancellationToken token = default)
		{
			try
			{
				while (true)
				{
					var buffer = ArrayPool<byte>.Shared.Rent(V2.Packet.MaxLength);

					try
					{
						var length = await transmissionChannel.ReceiveAsync(buffer, token);

						//TODO: handle bool return
						var result = await pipeline.ProcessAsync(buffer.AsMemory(0, length));
						Console.WriteLine($"Pipeline: {result}");
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(buffer);
					}
				}
			}
			catch (Exception)
			{
				//TODO: logger
			}
		}

		public void Dispose()
		{
			transmissionChannel.Dispose();
		}
	}
}