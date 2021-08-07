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
		private readonly MavCmd command;
		private readonly byte targetSystem;
		private readonly byte targetComponent;
		private readonly int period;

		private readonly Task background;

		public TargetCommandProgressHandler(ICommandClient client, MavCmd command, byte targetSystem, byte targetComponent, int period = 1000)
		{
			this.client = client;
			this.command = command;
			this.targetSystem = targetSystem;
			this.targetComponent = targetComponent;
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
					Command = command,
					Result = MavResult.InProgress,
					Progress = progress,
					TargetSystem = targetSystem,
					TargetComponent = targetComponent
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
				Command = command,
				Result = result,
				TargetSystem = targetSystem,
				TargetComponent = targetComponent
			});
		}

		public void Dispose()
		{
			cancellation.Dispose();
		}
	}
}