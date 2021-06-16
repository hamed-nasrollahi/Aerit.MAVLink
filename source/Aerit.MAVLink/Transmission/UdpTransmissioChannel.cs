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
		private readonly Socket writer = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		private readonly Socket reader = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		public UdpTransmissionChannel(IPEndPoint inbound, IPEndPoint outbound)
		{
			writer.Connect(outbound);

			reader.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
			reader.Bind(inbound);
		}

		public ValueTask<int> SendAsync(byte[] buffer, int length, CancellationToken token = default)
		{
			throw new NotImplementedException();
		}

		public ValueTask<int> ReceiveAsync(byte[] buffer, CancellationToken token = default)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			reader.Dispose();
			writer.Dispose();
		}
	}

	public sealed class TransmissionChannel : IDisposable
	{
		static readonly IPEndPoint IPv4Any = new(IPAddress.Any, IPEndPoint.MinPort);
		static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, IPEndPoint.MinPort);

		private readonly AddressFamily family;
		private readonly Socket socket;

		public TransmissionChannel(AddressFamily family = AddressFamily.InterNetwork)
		{
			this.family = family;

			socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
		}

		public TransmissionChannel(IPEndPoint localEP)
		{
			family = localEP.AddressFamily;

			socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
			socket.Bind(localEP);
		}

		private int disposed = 0;

		private void ThrowIfDisposed()
		{
			void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);

			if (disposed == 1)
			{
				ThrowObjectDisposedException();
			}
		}

		public ValueTask ConnectAsync(IPEndPoint endPoint, CancellationToken token = default)
		{
			ThrowIfDisposed();

			return socket.ConnectAsync(endPoint, token);
		}

		public async Task<int> SendAsync(byte[] buffer, int length, IPEndPoint? endPoint = null, CancellationToken token = default)
		{
			ThrowIfDisposed();

			var completionSource = new TaskCompletionSource<bool>();

			using var registration = token.Register(() => completionSource.TrySetResult(true));

			var send = endPoint is null
				? socket.SendAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None)
				: socket.SendToAsync(new ArraySegment<byte>(buffer, 0, length), SocketFlags.None, endPoint);

			if (send != await Task.WhenAny(send, completionSource.Task).ConfigureAwait(false))
			{
				throw new OperationCanceledException(token);
			}

			return await send;
		}

		// TODO: reuse completionSource for efficiency in Client.ListenAsync
		public async Task<(int length, IPEndPoint remoteEndPoint)> ReceiveAsync(byte[] buffer, CancellationToken token = default)
		{
			ThrowIfDisposed();

			var completionSource = new TaskCompletionSource<bool>();

			using var registration = token.Register(() => completionSource.TrySetResult(true));

			var receive = socket.ReceiveFromAsync(
			   new(buffer),
			   SocketFlags.None,
			   family == AddressFamily.InterNetwork ? IPv4Any : IPv6Any);

			if (receive != await Task.WhenAny(receive, completionSource.Task).ConfigureAwait(false))
			{
				throw new OperationCanceledException(token);
			}

			var result = await receive;

			return (result.ReceivedBytes, (IPEndPoint)result.RemoteEndPoint);
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