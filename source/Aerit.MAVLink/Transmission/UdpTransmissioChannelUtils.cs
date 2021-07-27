using System.Net;

namespace Aerit.MAVLink
{
	public static class UdpTransmissionChannelUtils
	{
		public static readonly IPEndPoint IPv4Any = new(IPAddress.Any, IPEndPoint.MinPort);
		public static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, IPEndPoint.MinPort);
	}
}