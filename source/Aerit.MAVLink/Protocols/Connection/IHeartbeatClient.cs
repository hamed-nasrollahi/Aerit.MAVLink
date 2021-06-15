using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Connection
{
	public interface IHeartbeatClient
	{
		Task SendAsync(Heartbeat message);
	}
}