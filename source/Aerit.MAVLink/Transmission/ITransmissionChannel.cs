#nullable enable

using System;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface ITransmissionChannel : IDisposable
	{
		Task<int> ReceiveAsync(byte[] buffer);

		Task SendAsync(byte[] buffer, int length);
	}
}