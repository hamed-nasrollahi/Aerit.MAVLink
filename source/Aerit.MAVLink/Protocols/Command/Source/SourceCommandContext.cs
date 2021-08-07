#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Command
{
    public sealed class SourceCommandContext : IAsyncDisposable
    {
        private readonly CancellationTokenSource cancellation = new();

        private readonly SourceCommandHandler handler;

		private readonly Task background;

		public SourceCommandContext(SourceCommandHandler handler, CommandLong commandLong)
        {
            this.handler = handler;

            CommandLong = commandLong;

            background = handler.RunAsync(this, cancellation.Token);
        }

        public CommandLong CommandLong { get; }

        public MavResult? Result { get; internal set; }

        public Task WaitAsync(CancellationToken token = default)
            => handler.WaitAsync(token);

        private int cancelled = 0;

        public void Cancel()
        {
            if (Result is not null && Result != MavResult.InProgress)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref cancelled, 1, 0) == 1)
            {
                return;
            }

            cancellation.Cancel();
        }

        public async ValueTask DisposeAsync()
        {
            await background;

            cancellation.Dispose();

			handler.Release();
		}
    }
}