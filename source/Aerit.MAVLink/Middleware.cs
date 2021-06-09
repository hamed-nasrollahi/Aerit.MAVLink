#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
    using PacketV1 = V1.Packet;
    using PacketV2 = V2.Packet;

    public interface IMiddleware
    {
    }

    public interface IBufferMiddleware : IMiddleware
    {
        Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer);
    }

    public interface IBufferMiddlewareOutput
    {
        IBufferMiddleware? Next { get; set; }
    }

    public interface IPacketMiddleware : IMiddleware
    {
        Task<bool> ProcessAsync(PacketV1 packet);

        Task<bool> ProcessAsync(PacketV2 packet);
    }

    public interface IPacketMiddlewareOutput
    {
        IPacketMiddleware? Next { get; set; }
    }

    public interface IMessageMiddleware<T> : IMiddleware
    {
        Task<bool> ProcessAsync(byte systemId, byte componentId, T message);
    }

    public interface IMessageMiddlewareOutput<T>
    {
        IMessageMiddleware<T>? Next { get; set; }
    }

    public class FilterMessageIdBufferMiddleware : IBufferMiddleware, IBufferMiddlewareOutput
    {
        public HashSet<uint>? Ids { get; init; }

        public IBufferMiddleware? Next { get; set; }

        public async Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer)
        {
            if (Next is null || Ids is null)
            {
                return false;
            }

            bool match = false;

            switch ((Magic)buffer.Span[0])
            {
                case Magic.V1:
                    {
                        var id = PacketV1.DeserializeMessageId(buffer);
                        if (id is not null && Ids.Contains(id.Value))
                        {
                            match = true;
                        }
                    }
                    break;

                case Magic.V2:
                    {
                        var id = PacketV2.DeserializeMessageId(buffer);
                        if (id is not null && Ids.Contains(id.Value))
                        {
                            match = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return match && await Next.ProcessAsync(buffer);
        }
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
                        var packet = PacketV1.Deserialize(buffer);
                        if (packet is null)
                        {
                            return Task.FromResult(false);
                        }

                        return Next.ProcessAsync(packet);
                    }

                case Magic.V2:
                    {
                        var packet = PacketV2.Deserialize(buffer);
                        if (packet is null)
                        {
                            return Task.FromResult(false);
                        }

                        return Next.ProcessAsync(packet);
                    }

                default:
                    return Task.FromResult(false);
            }
        }
    }

    public class PacketValidationMiddleware : IPacketMiddleware, IPacketMiddlewareOutput
    {
        public IPacketMiddleware? Next { get; set; }

        public Task<bool> ProcessAsync(PacketV1 packet)
        {
            if (Next is null)
            {
                return Task.FromResult(false);
            }

            if (!packet.Validate())
            {
                return Task.FromResult(false);
            }

            return Next.ProcessAsync(packet);
        }

        public Task<bool> ProcessAsync(PacketV2 packet)
        {
            if (Next is null)
            {
                return Task.FromResult(false);
            }

            if (!packet.Validate())
            {
                return Task.FromResult(false);
            }

            return Next.ProcessAsync(packet);
        }
    }

    public interface IPacketMapBranch : IPacketMiddleware
    {
        bool Eval(PacketV1 packet);

        bool Eval(PacketV2 packet);
    }

    public class PacketMapMiddleware : IPacketMiddleware
    {
        private readonly List<IPacketMapBranch> branches = new();

        public Task<bool> ProcessAsync(PacketV1 packet)
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

        public Task<bool> ProcessAsync(PacketV2 packet)
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