using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Connection
{
	//TODO: Similar to CommandAckEndpoint
	public sealed class HeartbeatEnpoint : IMessageMiddleware<Heartbeat>
	{
		public Task<bool> ProcessAsync(byte systemId, byte componentId, Heartbeat message)
		{
			return Task.FromResult(false);
		}
	}
}