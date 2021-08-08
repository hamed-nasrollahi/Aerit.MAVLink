#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Prometheus;

namespace Aerit.MAVLink
{
	public interface IPacketMiddleware : IMiddleware
	{
		Task<bool> ProcessAsync(V1.Packet packet, CancellationToken token);

		Task<bool> ProcessAsync(V2.Packet packet, CancellationToken token);
	}

	public interface IPacketMiddlewareOutput
	{
		IPacketMiddleware? Next { get; set; }
	}

	public class PacketMiddleware : IBufferMiddleware, IPacketMiddlewareOutput
	{
		private readonly ILogger<PacketMiddleware> logger;

		public PacketMiddleware(ILogger<PacketMiddleware> logger)
		{
			this.logger = logger;
		}

		public IEnumerable<uint>? Ids => null;

		public IPacketMiddleware? Next { get; set; }

		private static readonly Counter IncomingPacketsCount = Metrics
			.CreateCounter("mavlink_packets_incoming_total", "Number of incoming mavlink packets.", new CounterConfiguration()
			{
				LabelNames = new[] { "version" }
			});

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer, PipelineContext context, CancellationToken token)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			switch ((Magic)buffer.Span[0])
			{
				case Magic.V1:
					{
						var packet = V1.Packet.Deserialize(buffer);
						if (packet is null)
						{
							logger.LogWarning("Unable to deserialize V1 packet");

							return Task.FromResult(false);
						}

						IncomingPacketsCount.WithLabels("v1").Inc();

						return Next.ProcessAsync(packet, token);
					}

				case Magic.V2:
					{
						var packet = V2.Packet.Deserialize(buffer);
						if (packet is null)
						{
							logger.LogWarning("Unable to deserialize V2 packet");

							return Task.FromResult(false);
						}

						IncomingPacketsCount.WithLabels("v2").Inc();

						return Next.ProcessAsync(packet, token);
					}

				default:
					logger.LogWarning("Unknown packet received");

					return Task.FromResult(false);
			}
		}
	}

	public class PacketValidationMiddleware : IPacketMiddleware, IPacketMiddlewareOutput
	{
		private readonly ILogger<PacketValidationMiddleware> logger;

		public PacketValidationMiddleware(ILogger<PacketValidationMiddleware> logger)
		{
			this.logger = logger;
		}

		public IEnumerable<uint>? Ids => null;

		public IPacketMiddleware? Next { get; set; }

		private static readonly Counter InvalidPacketsCount = Metrics
			.CreateCounter("mavlink_packets_invalid_total", "Number of invalid mavlink packets.", new CounterConfiguration()
			{
				LabelNames = new[] { "version" }
			});

		public Task<bool> ProcessAsync(V1.Packet packet, CancellationToken token)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			if (!packet.Validate())
			{
				InvalidPacketsCount.WithLabels("v1").Inc();

				logger.LogWarning("Unable to validate {packet}", packet);

				return Task.FromResult(false);
			}

			return Next.ProcessAsync(packet, token);
		}

		public Task<bool> ProcessAsync(V2.Packet packet, CancellationToken token)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			if (!packet.Validate())
			{
				InvalidPacketsCount.WithLabels("v2").Inc();

				logger.LogWarning("Unable to validate {packet}", packet);

				return Task.FromResult(false);
			}

			return Next.ProcessAsync(packet, token);
		}
	}

	public interface IPacketMapBranch : IPacketMiddleware
	{
		bool Eval(V1.Packet packet);

		bool Eval(V2.Packet packet);
	}

	public partial class PacketMapMiddleware : IPacketMiddleware
	{
		private readonly HashSet<uint> ids = new();

		private readonly List<IPacketMapBranch> branches = new();
		
		private readonly ILoggerFactory loggerFactory;

		public PacketMapMiddleware(ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory;
		}

		public IEnumerable<uint>? Ids => ids;

		public Task<bool> ProcessAsync(V1.Packet packet, CancellationToken token)
		{
			foreach (var branch in branches)
			{
				token.ThrowIfCancellationRequested();

				if (branch.Eval(packet))
				{
					return branch.ProcessAsync(packet, token);
				}
			}

			return Task.FromResult(false);
		}

		public Task<bool> ProcessAsync(V2.Packet packet, CancellationToken token)
		{
			foreach (var branch in branches)
			{
				token.ThrowIfCancellationRequested();

				if (branch.Eval(packet))
				{
					return branch.ProcessAsync(packet, token);
				}
			}

			return Task.FromResult(false);
		}

		public PacketMapMiddleware Add<T>(Func<PipelineBuilder<IPacketMapBranch>, (PipelineBuilder<IPacketMapBranch> builder, T last)> builder)
			where T : IMiddleware
		{
			var branch = builder(BranchBuilder.Create(loggerFactory));

			foreach (var id in branch.builder.Ids)
			{
				ids.Add(id);
			}

			branches.Add(branch.Build());

			return this;
		}
	}
}