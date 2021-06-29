using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Command
{
	public sealed class CommandAckEnpoint : IMessageMiddleware<CommandAck>
	{
		private readonly SourceCommandHandlerRegistry handlers;

		public CommandAckEnpoint(SourceCommandHandlerRegistry handlers)
		{
			this.handlers = handlers;
		}

		public async Task<bool> ProcessAsync(byte systemId, byte componentId, CommandAck message, CancellationToken token)
		{
			if (!handlers.TryGet(systemId, componentId, message.Command, out var handler))
			{
				return false;
			}

			return await handler!.EnqueueAsync(message, token);
		}
	}
}