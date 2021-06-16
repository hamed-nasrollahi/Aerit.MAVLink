#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface ITransmissionChannel : IDisposable
	{
		ValueTask<int> SendAsync(byte[] buffer, int length, CancellationToken token = default);

		ValueTask<int> ReceiveAsync(byte[] buffer, CancellationToken token = default);
	}
}