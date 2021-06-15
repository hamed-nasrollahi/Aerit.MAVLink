#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface ITransmissionChannel : IDisposable
	{
		ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default);

		ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default);
	}
}