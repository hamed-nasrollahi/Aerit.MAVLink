using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
	public interface ITransmissionChannelOut : ITransmissionChannel 
	{
		ValueTask ConnectAsync(CancellationToken token = default);
	}
}