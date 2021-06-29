using System;
using System.Threading;

namespace Aerit.MAVLink.Protocols.Command
{
	public sealed class TargetCommandHandler : IDisposable
	{
		private readonly CancellationTokenSource cancellation = new();

		private readonly (byte systemId, byte componentId, MavCmd command) key;
		private readonly TargetCommandHandlerRegistry registry;

		public TargetCommandHandler((byte systemId, byte componentId, MavCmd command) key, TargetCommandHandlerRegistry registry)
		{
			this.key = key;
			this.registry = registry;
		}

		private int acquired = 0;

		public bool TryAcquire()
			=> Interlocked.CompareExchange(ref acquired, 1, 0) == 0;

		private int cancelled = 0;

		public void Cancel()
		{
			if (Interlocked.CompareExchange(ref cancelled, 1, 0) == 0)
			{
				cancellation.Cancel();
			}
		}

		public void Dispose()
		{
			cancellation.Dispose();

			registry.Release(key);
		}
	}
}