#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink
{
	public interface IPacketMiddleware : IMiddleware
	{
		Task<bool> ProcessAsync(V1.Packet packet);

		Task<bool> ProcessAsync(V2.Packet packet);
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

		public IPacketMiddleware? Next { get; set; }

		public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			switch ((Magic)buffer.Span[0])
			{
				case Magic.V1:
					{
						//TODO: metric v1 incoming

						var packet = V1.Packet.Deserialize(buffer);
						if (packet is null)
						{
							logger.LogWarning("Unable to deserialize V1 packet");

							return Task.FromResult(false);
						}

						//TODO: metric v1 outgoing

						return Next.ProcessAsync(packet);
					}

				case Magic.V2:
					{
						//TODO: metric v2 incoming

						var packet = V2.Packet.Deserialize(buffer);
						if (packet is null)
						{
							logger.LogWarning("Unable to deserialize V2 packet");
							//TODO: metric
							return Task.FromResult(false);
						}

						//TODO: metric v2 outgoing

						return Next.ProcessAsync(packet);
					}

				default:
					logger.LogWarning("Unknown packet received");
					//TODO: metric unknown
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

		public IPacketMiddleware? Next { get; set; }

		public Task<bool> ProcessAsync(V1.Packet packet)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			//TODO: metric v1 incoming

			if (!packet.Validate())
			{
				logger.LogWarning("Unable to validate {packet}", packet);

				return Task.FromResult(false);
			}

			//TODO: metric v1 outgoing

			return Next.ProcessAsync(packet);
		}

		public Task<bool> ProcessAsync(V2.Packet packet)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			//TODO: metric v2 incoming

			if (!packet.Validate())
			{
				logger.LogWarning("Unable to validate {packet}", packet);

				return Task.FromResult(false);
			}

			//TODO: metric v2 outgoing

			return Next.ProcessAsync(packet);
		}
	}

	public interface IPacketMapBranch : IPacketMiddleware
	{
		bool Eval(V1.Packet packet);

		bool Eval(V2.Packet packet);
	}

	public class PacketMapMiddleware : IPacketMiddleware
	{
		private readonly List<IPacketMapBranch> branches = new();
		
		private readonly ILoggerFactory loggerFactory;

		public PacketMapMiddleware(ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory;
		}

		public Task<bool> ProcessAsync(V1.Packet packet)
		{
			//TODO: metric v1 incoming

			foreach (var branch in branches)
			{
				if (branch.Eval(packet))
				{
					//TODO: metric v1 outgoing

					return branch.ProcessAsync(packet);
				}
			}

			return Task.FromResult(false);
		}

		public Task<bool> ProcessAsync(V2.Packet packet)
		{
			//TODO: metric v2 incoming

			foreach (var branch in branches)
			{
				if (branch.Eval(packet))
				{
					//TODO: metric v2 outgoing

					return branch.ProcessAsync(packet);
				}
			}

			return Task.FromResult(false);
		}

		public PacketMapMiddleware Add<T>(Func<PipelineBuilder<IPacketMapBranch>, (PipelineBuilder<IPacketMapBranch> builder, T last)> builder)
			where T : IMiddleware
		{
			branches.Add(builder(BranchBuilder.Create(loggerFactory)).Build());

			return this;
		}
	}
}