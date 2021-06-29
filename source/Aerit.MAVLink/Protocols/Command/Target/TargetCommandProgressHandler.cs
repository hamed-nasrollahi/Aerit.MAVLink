using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Command
{
	public sealed class TargetCommandProgressHandler : IDisposable
	{
		private readonly CancellationTokenSource cancellation = new();
		private readonly Channel<byte> channel = Channel.CreateUnbounded<byte>();

		private readonly ICommandClient client;
		private readonly int period;

		private readonly Task background;

		public TargetCommandProgressHandler(ICommandClient client, int period)
		{
			this.client = client;
			this.period = period;

			background = RunAsync(cancellation.Token);
		}

		private byte progress = 0;

		private async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				using var timeoutCancellation = new CancellationTokenSource();

				timeoutCancellation.CancelAfter(period);

				try
				{
					if (!await channel.Reader.WaitToReadAsync(timeoutCancellation.Token).ConfigureAwait(false))
					{
						return;
					}

					var progress = await channel.Reader.ReadAsync(CancellationToken.None);

					if (this.progress == 255 || progress > this.progress)
					{
						this.progress = progress;
					}
				}
				catch (OperationCanceledException)
				{
					if (token.IsCancellationRequested)
					{
						return;
					}
				}

				await client.SendAsync(new CommandAck
				{
					Result = MavResult.InProgress,
					Progress = progress,
					TargetSystem = 0, //TODO
					TargetComponent = 0 //TODO
				});
			}
		}

		public ValueTask AdvanceAsync(byte progress)
			=> channel.Writer.WriteAsync(progress);

		private int completed = 0;

		public async Task CompleteAsync(MavResult result)
		{
			if (Interlocked.CompareExchange(ref completed, 1, 0) == 0)
			{
				cancellation.Cancel();
			}

			await background.ConfigureAwait(false);

			await client.SendAsync(new CommandAck
			{
				Result = result,
				TargetSystem = 0, //TODO
				TargetComponent = 0 //TODO
			});
		}

		public void Dispose()
		{
			cancellation.Dispose();
		}
	}
}