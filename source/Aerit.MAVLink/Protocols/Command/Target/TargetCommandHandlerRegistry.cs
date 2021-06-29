#nullable enable

using System.Collections.Concurrent;

namespace Aerit.MAVLink.Protocols.Command
{
	public sealed class TargetCommandHandlerRegistry
	{
		private readonly ConcurrentDictionary<(byte systemId, byte componentId, MavCmd command), TargetCommandHandler> handlers = new();

		private readonly ICommandClient client;

		public TargetCommandHandlerRegistry(ICommandClient client)
		{
			this.client = client;
		}

		public bool TryGet(byte systemId, byte componentId, MavCmd command, out TargetCommandHandler? handler)
			=> handlers.TryGetValue((systemId, componentId, command), out handler);

		public bool TryAcquire(byte systemId, byte componentId, MavCmd command, out TargetCommandHandler? handler)
		{
			handler = null;

			var value = handlers.GetOrAdd((systemId, componentId, command), key => new(key, this));
			if (value is null)
			{
				return false;
			}

			if (value.TryAcquire())
			{
				handler = value;

				return true;
			}

			return false;
		}

		internal void Release((byte systemId, byte componentId, MavCmd command) key)
		{
			handlers.TryRemove(key, out _);
		}
	}
}