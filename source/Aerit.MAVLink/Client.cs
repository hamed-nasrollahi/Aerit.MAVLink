#nullable enable

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NodaTime;

using Prometheus;

namespace Aerit.MAVLink
{
	using Utils;
	using V2;

	using Protocols.Connection;
	using Protocols.Command;

	public sealed partial class Client : IHeartbeatClient, ICommandClient, IDisposable
	{
		public class Options
		{
			public byte SystemId { get; set; }

			public MavComponent ComponentId { get; set; }
		}

		private readonly ILogger<Client> logger;
		private readonly ITransmissionChannel transmissionChannel;

		private readonly SourceCommandHandlerRegistry sourceCommandHandlers;

		private readonly Instant boot = SystemClock.Instance.GetCurrentInstant();

		public Client(ILogger<Client> logger, ITransmissionChannel transmissionChannel, IOptions<Options> options)
		{
			this.logger = logger;
			this.transmissionChannel = transmissionChannel;

			sourceCommandHandlers = new(this);

			SystemId = options.Value.SystemId;
			ComponentId = options.Value.ComponentId;
		}

		public byte SystemId { get; }

		public MavComponent ComponentId { get; }

		public uint TimeBootMs() => (uint)(SystemClock.Instance.GetCurrentInstant() - boot).TotalMilliseconds;

		private byte[] InitializeBuffer(uint messageID)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(V2.Packet.MaxLength);

			buffer[0] = (byte)Magic.V2;

			buffer[2] = 0x00;
			buffer[3] = 0x00;
			buffer[4] = 0x00;
			buffer[5] = SystemId;
			buffer[6] = (byte)ComponentId;

			buffer[7] = (byte)messageID;
			buffer[8] = (byte)(messageID >> 8);
			buffer[9] = (byte)(messageID >> 16);

			return buffer;
		}

		private SpinLock sequenceLock = new();
		private byte sequence = 0;

		private byte GetSequence()
		{
			bool entered = false;
			try
			{
				sequenceLock.Enter(ref entered);

				var result = sequence;

				if (sequence == 255)
				{
					sequence = 0;
				}
				else
				{
					sequence++;
				}

				return result;
			}
			finally
			{
				if (entered)
				{
					sequenceLock.Exit();
				}
			}
		}

		private int FinalizeBuffer(byte[] buffer, byte crcExtra)
		{
			buffer[4] = GetSequence();

			var crc = Checksum.Seed;

			for (var i = 1; i <= (9 + buffer[1]); i++)
			{
				crc = Checksum.Compute(buffer[i], crc);
			}

			crc = Checksum.Compute(crcExtra, crc);

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

		private async Task SendAsync(byte[] buffer, byte crcExtra)
		{
			try
			{
				var length = FinalizeBuffer(buffer, crcExtra);

				await transmissionChannel.SendAsync(buffer, length).ConfigureAwait(false);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		public SourceCommandContext? Submit(CommandLong command)
		{
			if (sourceCommandHandlers.TryAcquire(command.TargetSystem, command.TargetComponent, command.Command, out var handler))
			{
				return new SourceCommandContext(handler!, command);
			}

			return null;
		}

		public CommandAckEnpoint CommandAckEnpoint()
			=> new(sourceCommandHandlers);

		private static readonly Counter IncomingBuffersCount = Metrics
			.CreateCounter("mavlink_buffers_incoming_total", "Number of incoming mavlink buffers.");

		private static readonly Counter ProcessedBuffersCount = Metrics
			.CreateCounter("mavlink_buffers_processed_total", "Number of mavlink buffers processed.");

		public async Task ListenAsync(Pipeline pipeline, CancellationToken token = default)
		{
			try
			{
				var completionSource = new TaskCompletionSource<bool>();

				using var registration = token.Register(() => completionSource.TrySetResult(true));

				var context = new PipelineContext(
					Target: (SystemId, ComponentId),
					Ids: pipeline.Ids);

				while (true)
				{
					var buffer = ArrayPool<byte>.Shared.Rent(V2.Packet.MaxLength);

					try
					{
						var receive = transmissionChannel.ReceiveAsync(buffer);
						if (receive != await Task.WhenAny(receive, completionSource.Task).ConfigureAwait(false))
						{
							break;
						}

						IncomingBuffersCount.Inc();

						if (await pipeline.First.ProcessAsync(buffer.AsMemory(0, receive.Result), context, token))
						{
							ProcessedBuffersCount.Inc();
						}
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(buffer);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Exception caught");
			}
		}

		public void Dispose()
		{
			transmissionChannel.Dispose();
		}
	}
}