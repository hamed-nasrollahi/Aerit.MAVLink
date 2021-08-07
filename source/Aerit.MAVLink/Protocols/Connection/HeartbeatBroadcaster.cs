using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Connection
{
	public sealed class HeartbeatBroadcaster : IAsyncDisposable
	{
		private readonly CancellationTokenSource cancellation = new();
		private readonly Channel<MavState> channel = Channel.CreateUnbounded<MavState>();

		private readonly IHeartbeatClient client;
		private readonly MavType type;
		private readonly uint customMode;
		private readonly MavAutopilot autopilot;
		private readonly MavModeFlag baseMode;
		private readonly int period;

		private readonly Task background;

		public HeartbeatBroadcaster(IHeartbeatClient client, MavType type, uint customMode = 0, MavAutopilot autopilot = MavAutopilot.Invalid, MavModeFlag baseMode = 0, int period = 1000)
		{
			this.client = client;
			this.type = type;
			this.customMode = customMode;
			this.autopilot = autopilot;
			this.baseMode = baseMode;
			this.period = period;

			background = RunAsync(cancellation.Token);
		}

		private MavState state = MavState.Uninit;

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

					state = await channel.Reader.ReadAsync(CancellationToken.None);
				}
				catch (OperationCanceledException)
				{
					if (token.IsCancellationRequested)
					{
						return;
					}
				}

				await client.SendAsync(new Heartbeat
				{
					CustomMode = customMode,
					Type = type,
					Autopilot = autopilot,
					BaseMode = baseMode,
					SystemStatus = state,
					MavlinkVersion = Version.Minimal
				});
			}
		}

		public ValueTask UpdateAsync(MavState state)
			=> channel.Writer.WriteAsync(state);

		public async ValueTask DisposeAsync()
		{
			cancellation.Cancel();

			await background.ConfigureAwait(false);

			cancellation.Dispose();
		}
	}
}