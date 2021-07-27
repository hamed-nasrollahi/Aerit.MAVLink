#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Aerit.MAVLink
{
	using static UdpTransmissionChannelUtils;

	public sealed class UdpTransmissionChannelIn : ITransmissionChannelIn
	{
		public class Options
		{
			public string? Uri { get; set; }
		}

		private readonly AddressFamily family;
		private readonly Socket socket;

		public UdpTransmissionChannelIn(IOptions<Options> options)
		{
			var localEP = IPEndPoint.Parse(options.Value.Uri ?? throw new ArgumentNullException("Options.Uri"));

			family = localEP.AddressFamily;

			socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
			socket.Bind(localEP);
		}

		private int active = 0;
		private int disposed = 0;

		private IPEndPoint? endPoint = null;

		public async Task<int> ReceiveAsync(byte[] buffer)
		{
			if (disposed == 1)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

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

			return socket.SendToAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None, endPoint!);
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