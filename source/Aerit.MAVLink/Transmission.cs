#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
    public interface ITransmissionChannel : IDisposable
    {
        Task SendAsync(byte[] buffer, int length);

        Task<byte[]?> ReceiveAsync();

        void Close();
    }

    public sealed class UdpTransmissioChannel : ITransmissionChannel
    {
        private readonly UdpClient client;

        private int closed = 0;

        public UdpTransmissioChannel(IPEndPoint host)
        {
            client = new UdpClient(host);
        }

        public Task SendAsync(byte[] buffer, int length)
        {
            if (closed == 1)
            {
                return Task.CompletedTask;
            }

            return client.SendAsync(buffer, length);
        }

        public async Task<byte[]?> ReceiveAsync()
        {
            if (closed == 1)
            {
                return null;
            }

            try
            {
                var result = await client.ReceiveAsync();

                return result.Buffer;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref closed, 1, 0) == 0)
            {
                client.Dispose();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}