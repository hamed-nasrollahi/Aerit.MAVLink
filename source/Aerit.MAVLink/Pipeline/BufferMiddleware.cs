#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Prometheus;

namespace Aerit.MAVLink
{
	using static Target;

	public interface IBufferMiddleware : IMiddleware
	{
		Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, CancellationToken token);
	}

	public interface IBufferMiddlewareOutput
	{
		IBufferMiddleware? Next { get; set; }
	}

	public class MatchBufferMiddleware : IBufferMiddleware, IBufferMiddlewareOutput
	{
		public HashSet<uint>? Ids { get; init; }

		public (byte? systemId, MavComponent? componentId)? Target { get; init; }

		public IBufferMiddleware? Next { get; set; }

		private static readonly Counter MatchedBuffersCount = Metrics
			.CreateCounter("mavlink_buffers_matched_total", "Number of mavlink buffers matched.");

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			bool match = false;

			switch ((Magic)buffer.Span[0])
			{
				case Magic.V1:
					{
						var id = V1.Packet.DeserializeMessageId(buffer);
						if (id is null)
						{
							break;
						}

						if (Ids is not null && !Ids.Contains(id.Value))
						{
							break;
						}

                        if (Target is not null && !Match(id.Value, buffer.Span[6..], Target.Value.systemId, (byte?)Target.Value.componentId))
						{
							break;
						}

						match = true;
					}
					break;

				case Magic.V2:
					{
						var id = V2.Packet.DeserializeMessageId(buffer);
						if (id is null)
						{
							break;
						}

						if (Ids is not null && !Ids.Contains(id.Value))
						{
							break;
						}

                        if (Target is not null && !Match(id.Value, buffer.Span[10..], Target.Value.systemId, (byte?)Target.Value.componentId))
						{
							break;
						}

						match = true;
					}
					break;

				default:
					break;
			}

			if (!match)
			{
				return Task.FromResult(false);
			}

			MatchedBuffersCount.Inc();

			return Next.ProcessAsync(buffer, token);
		}
	}

	public class BufferEndpoint : IBufferMiddleware
	{
		private readonly Func<ReadOnlyMemory<byte>, bool> process;

		public BufferEndpoint(Func<ReadOnlyMemory<byte>, bool> process)
		{
			this.process = process;
		}

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
			=> Task.FromResult(process(buffer));
	}

	public class BufferAsyncEndpoint : IBufferMiddleware
	{
		private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task<bool>> process;

		public BufferAsyncEndpoint(Func<ReadOnlyMemory<byte>, CancellationToken, Task<bool>> process)
		{
			this.process = process;
		}

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
			=> process(buffer, token);
	}
}