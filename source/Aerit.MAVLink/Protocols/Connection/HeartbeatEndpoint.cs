using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Connection
{
	//TODO: Similar to CommandAckEndpoint
	public sealed class HeartbeatEnpoint : IMessageMiddleware<Heartbeat>
	{
		public Task<bool> ProcessAsync(byte systemId, byte componentId, Heartbeat message, CancellationToken token)
		{
			return Task.FromResult(false);
		}
	}
}