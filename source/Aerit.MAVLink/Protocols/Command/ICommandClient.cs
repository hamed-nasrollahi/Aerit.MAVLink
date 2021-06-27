using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Command
{
	public interface ICommandClient
    {
        Task SendAsync(CommandLong message);

        Task SendAsync(CommandCancel message);

        Task SendAsync(CommandAck message);
    }
}