#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public sealed class UdpTransmissionChannel : ITransmissionChannel
	{
		static readonly IPEndPoint IPv4Any = new(IPAddress.Any, IPEndPoint.MinPort);
		static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, IPEndPoint.MinPort);

		private readonly AddressFamily family;
		private readonly Socket socket;

		public UdpTransmissionChannel(AddressFamily family = AddressFamily.InterNetwork)
		{
			this.family = family;

			socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
		}

		public UdpTransmissionChannel(IPEndPoint localEP)
		{
			family = localEP.AddressFamily;

			socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
			socket.Bind(localEP);
		}

		private int active = 0;
		private int disposed = 0;

		public ValueTask ConnectAsync(IPEndPoint endPoint, CancellationToken token = default)
		{
			if (disposed == 1)
			{
				return ValueTask.FromException(new ObjectDisposedException(GetType().FullName));
			}

			if (Interlocked.CompareExchange(ref active, 1, 0) == 1)
			{
				return ValueTask.CompletedTask;
			}

			return socket.ConnectAsync(endPoint, token);
		}

		private IPEndPoint? endPoint = null;

		public async Task<int> ReceiveAsync(byte[] buffer)
		{
			if (disposed == 1)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			//SocketErrorCode.ConnectionRefused

			var result = await socket.ReceiveFromAsync(
			   new(buffer),
			   SocketFlags.None,
			   family == AddressFamily.InterNetwork ? IPv4Any : IPv6Any);

			if (Interlocked.CompareExchange(ref active, 1, 0) == 0)
			{
				endPoint = (IPEndPoint)result.RemoteEndPoint;
			}

			return result.ReceivedBytes;
		}

		public Task SendAsync(byte[] buffer, int length)
		{
			if (disposed == 1)
			{
				return Task.FromException(new ObjectDisposedException(GetType().FullName));
			}

			if (active == 0)
			{
				return Task.CompletedTask;
			}

			return endPoint is null
				? socket.SendAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None)
				: socket.SendToAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None, endPoint);
		}

		public void Close() => Dispose();

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Dispose();
			}
		}
	}
}