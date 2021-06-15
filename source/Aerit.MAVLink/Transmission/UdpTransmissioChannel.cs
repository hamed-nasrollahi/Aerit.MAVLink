#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public sealed class UdpTransmissioChannel2 : ITransmissionChannel
	{
		private readonly Socket writer = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		private readonly Socket reader = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		public UdpTransmissioChannel2(IPEndPoint inbound, IPEndPoint outbound)
		{
			writer.Connect(outbound);

			reader.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
			reader.Bind(inbound);
		}

		public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
			=> writer.SendAsync(buffer, SocketFlags.None, token);

		public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default)
			=> reader.ReceiveAsync(buffer, SocketFlags.None, token);

        public void Dispose()
		{
			reader.Dispose();
			writer.Dispose();
		}
	}
}