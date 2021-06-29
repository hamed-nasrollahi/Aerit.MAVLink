#nullable enable

using System.Collections.Concurrent;

namespace Aerit.MAVLink.Protocols.Command
{
    public sealed class SourceCommandHandlerRegistry
    {
		private const int sequenceTimeout = 1000;
		private const byte retry = 10;
		private const int inProgressTimeout = 5000;

        private readonly ConcurrentDictionary<(byte systemId, byte componentId, MavCmd command), SourceCommandHandler> handlers = new();

        private readonly ICommandClient client;

        public SourceCommandHandlerRegistry(ICommandClient client)
        {
            this.client = client;
        }

        public bool TryGet(byte systemId, byte componentId, MavCmd command, out SourceCommandHandler? handler)
            => handlers.TryGetValue((systemId, componentId, command), out handler);

		public bool TryAcquire(byte systemId, byte componentId, MavCmd command, out SourceCommandHandler? handler)
		{
			handler = null;

			var value = handlers.GetOrAdd((systemId, componentId, command), key => new(client, sequenceTimeout, retry, inProgressTimeout));
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
	}
}