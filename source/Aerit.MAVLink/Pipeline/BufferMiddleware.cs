#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface IBufferMiddleware : IMiddleware
    {
        Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer);
    }

    public interface IBufferMiddlewareOutput
    {
        IBufferMiddleware? Next { get; set; }
    }

    public class FilterMessageIdBufferMiddleware : IBufferMiddleware, IBufferMiddlewareOutput
    {
        public HashSet<uint>? Ids { get; init; }

        public IBufferMiddleware? Next { get; set; }

        public Task<bool> ProcessAsync(ReadOnlyMemory<byte> buffer)
        {
            if (Next is null || Ids is null)
            {
				return Task.FromResult(false);
            }

            bool match = false;

            switch ((Magic)buffer.Span[0])
            {
                case Magic.V1:
                    {
                        var id = V1.Packet.DeserializeMessageId(buffer);
                        if (id is not null && Ids.Contains(id.Value))
                        {
                            match = true;
                        }
                    }
                    break;

                case Magic.V2:
                    {
                        var id = V2.Packet.DeserializeMessageId(buffer);
                        if (id is not null && Ids.Contains(id.Value))
                        {
                            match = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            if (!match)
            {
				return Task.FromResult(false);
			}

            return Next.ProcessAsync(buffer);
        }
    }
}