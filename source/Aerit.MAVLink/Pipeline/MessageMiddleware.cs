#nullable enable

using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface IMessageMiddleware<T> : IMiddleware
    {
        Task<bool> ProcessAsync(byte systemId, byte componentId, T message);
    }

    public interface IMessageMiddlewareOutput<T>
    {
        IMessageMiddleware<T>? Next { get; set; }
    }
}