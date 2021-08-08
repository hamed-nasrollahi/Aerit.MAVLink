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
		Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, PipelineContext context, CancellationToken token);
	}

	public interface IBufferMiddlewareOutput
	{
		IBufferMiddleware? Next { get; set; }
	}

	public class FilterBufferMiddleware : IBufferMiddleware, IBufferMiddlewareOutput
	{
		private readonly bool target;
		private readonly bool ids;

		public FilterBufferMiddleware(bool target = true, bool ids = true)
		{
			this.target = target;
			this.ids = ids;
		}

		public IEnumerable<uint>? Ids => null;

		public IBufferMiddleware? Next { get; set; }

		private static readonly Counter MatchedBuffersCount = Metrics
			.CreateCounter("mavlink_buffers_matched_total", "Number of mavlink buffers matched.");

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, PipelineContext context, CancellationToken token)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			if (ids || target)
			{
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

							if (ids && !context.Ids.Contains(id.Value))
							{
								break;
							}

							if (target && !Match(id.Value, buffer.Span[6..], context.Target.systemId, context.Target.componentId))
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

							if (ids && !context.Ids.Contains(id.Value))
							{
								break;
							}

							if (target && !Match(id.Value, buffer.Span[10..], context.Target.systemId, context.Target.componentId))
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
			}

			MatchedBuffersCount.Inc();

			return Next.ProcessAsync(buffer, context, token);
		}
	}

	public class BufferEndpoint : IBufferMiddleware
	{
		private readonly Func<ReadOnlyMemory<byte>, PipelineContext, bool> process;

		public BufferEndpoint(Func<ReadOnlyMemory<byte>, PipelineContext, bool> process)
		{
			this.process = process;
		}

		public IEnumerable<uint>? Ids => null;

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, PipelineContext context, CancellationToken token)
			=> Task.FromResult(process(buffer, context));
	}

	public class BufferAsyncEndpoint : IBufferMiddleware
	{
		private readonly Func<ReadOnlyMemory<byte>, PipelineContext, CancellationToken, Task<bool>> process;

		public BufferAsyncEndpoint(Func<ReadOnlyMemory<byte>, PipelineContext, CancellationToken, Task<bool>> process)
		{
			this.process = process;
		}

		public IEnumerable<uint>? Ids => null;

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, PipelineContext context, CancellationToken token)
			=> process(buffer, context, token);
	}
}