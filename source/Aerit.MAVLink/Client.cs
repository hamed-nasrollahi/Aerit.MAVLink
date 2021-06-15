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

		private int FinalizeBuffer(Memory<byte> buffer)
		{
			var span = buffer.Span;

			span[4] = sequence;

			var crc = Checksum.Seed;

			for (var i = 1; i <= (9 + span[1]); i++)
			{
				crc = Checksum.Compute(span[i], crc);
			}

			crc = Checksum.Compute(span[10 + span[1]], crc);

			span[10 + span[1]] = (byte)crc;
			span[11 + span[1]] = (byte)(crc >> 8);

			var length = 12 + span[1];

			if (((IncompatibilityFlags)span[2] & IncompatibilityFlags.Signed) != 0x00)
			{
				byte linkId = 0x00;
				span[12 + span[1]] = linkId;

				ulong timeStamp48 = 0;

				span[13 + span[1]] = (byte)timeStamp48;
				span[14 + span[1]] = (byte)(timeStamp48 >> 8);
				span[15 + span[1]] = (byte)(timeStamp48 >> 16);
				span[16 + span[1]] = (byte)(timeStamp48 >> 24);
				span[17 + span[1]] = (byte)(timeStamp48 >> 32);
				span[18 + span[1]] = (byte)(timeStamp48 >> 40);

				//var signature = Signature.Compute(key, span[..(12 + span[1] + 1 + 6)]);
				ulong signature = 0;

				span[19 + span[1]] = (byte)signature;
				span[20 + span[1]] = (byte)(signature >> 8);
				span[21 + span[1]] = (byte)(signature >> 16);
				span[22 + span[1]] = (byte)(signature >> 24);
				span[23 + span[1]] = (byte)(signature >> 32);
				span[24 + span[1]] = (byte)(signature >> 40);

				length += 13;
			}

			return length;
		}

		private async Task SendAsync(IMemoryOwner<byte> buffer)
		{
			try
			{
				var length = FinalizeBuffer(buffer.Memory);

				await transmissionChannel.SendAsync(buffer.Memory.Slice(0, length)).ConfigureAwait(false);

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
				buffer.Dispose();
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
					using var buffer = MemoryPool<byte>.Shared.Rent(V2.Packet.MaxLength);

					await transmissionChannel.ReceiveAsync(buffer.Memory, token);

					//TODO: handle bool return
					await pipeline.ProcessAsync(buffer.Memory);
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