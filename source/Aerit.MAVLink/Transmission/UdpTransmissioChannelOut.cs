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

	public sealed class UdpTransmissionChannelOut : ITransmissionChannelOut
	{
		public class Options
		{
			public string? Uri { get; set; }
		}

		private readonly IPEndPoint endPoint;
		private readonly Socket socket;

		public UdpTransmissionChannelOut(IOptions<Options> options)
		{
			endPoint = IPEndPoint.Parse(options.Value.Uri ?? throw new ArgumentNullException("Options.Uri"));

			socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
		}

		private int active = 0;
		private int disposed = 0;

		public ValueTask ConnectAsync(CancellationToken token = default)
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

		public async Task<int> ReceiveAsync(byte[] buffer)
		{
			if (disposed == 1)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			var result = await socket.ReceiveFromAsync(
			   new(buffer),
			   SocketFlags.None,
			   endPoint.AddressFamily == AddressFamily.InterNetwork ? IPv4Any : IPv6Any);

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

			return socket.SendAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None);
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