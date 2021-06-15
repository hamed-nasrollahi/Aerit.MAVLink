#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
						var packet = V1.Packet.Deserialize(buffer);
						if (packet is null)
						{
							//TODO: metric
							return Task.FromResult(false);
						}

						return Next.ProcessAsync(packet);
					}

				case Magic.V2:
					{
						var packet = V2.Packet.Deserialize(buffer);
						if (packet is null)
						{
							//TODO: metric
							return Task.FromResult(false);
						}

						return Next.ProcessAsync(packet);
					}

				default:
					//TODO: metric
					return Task.FromResult(false);
			}
		}
	}

	public class PacketValidationMiddleware : IPacketMiddleware, IPacketMiddlewareOutput
	{
		public IPacketMiddleware? Next { get; set; }

		public Task<bool> ProcessAsync(V1.Packet packet)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			if (!packet.Validate())
			{
				//TODO: metric
				return Task.FromResult(false);
			}

			return Next.ProcessAsync(packet);
		}

		public Task<bool> ProcessAsync(V2.Packet packet)
		{
			if (Next is null)
			{
				return Task.FromResult(false);
			}

			if (!packet.Validate())
			{
				//TODO: metric
				return Task.FromResult(false);
			}

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

		public Task<bool> ProcessAsync(V1.Packet packet)
		{
			foreach (var branch in branches)
			{
				if (branch.Eval(packet))
				{
					return branch.ProcessAsync(packet);
				}
			}

			return Task.FromResult(false);
		}

		public Task<bool> ProcessAsync(V2.Packet packet)
		{
			foreach (var branch in branches)
			{
				if (branch.Eval(packet))
				{
					return branch.ProcessAsync(packet);
				}
			}

			return Task.FromResult(false);
		}

		public PacketMapMiddleware Add(Func<IPacketMapBranch> branch)
		{
			branches.Add(branch());

			return this;
		}

		public static PacketMapMiddleware Create() => new();
	}
}