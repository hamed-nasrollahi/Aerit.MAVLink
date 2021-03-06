#nullable enable

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aerit.MAVLink.Protocols.Command
{
	public sealed class SourceCommandHandler : IDisposable
	{
		private readonly SemaphoreSlim cancellationSemaphore = new(0, 1);
		private readonly SemaphoreSlim completionSemaphore = new(0, 1);

		private readonly Channel<CommandAck> channel = Channel.CreateUnbounded<CommandAck>();

		private readonly ICommandClient client;
		private readonly int sequenceTimeout;
		private readonly byte retry;
		private readonly int inProgressTimeout;

		public SourceCommandHandler(ICommandClient client, int sequenceTimeout, byte retry, int inProgressTimeout)
		{
			this.client = client;
			this.sequenceTimeout = sequenceTimeout;
			this.retry = retry;
			this.inProgressTimeout = inProgressTimeout;
		}

		private int acquired = 0;
		private bool enabled = false;

		public bool TryAcquire()
			=> Interlocked.CompareExchange(ref acquired, 1, 0) == 0;

		public void Release()
		{
			acquired = 0;
		}

		public async ValueTask<bool> EnqueueAsync(CommandAck ack, CancellationToken token)
		{
			if (!enabled)
			{
				return false;
			}

			await channel.Writer.WriteAsync(ack, token).ConfigureAwait(false);

			return true;
		}

		private async Task RunCancellationAsync(SourceCommandContext context, CancellationToken token)
		{
			var cancel = new CommandCancel()
			{
				Command = context.CommandLong.Command,
				TargetSystem = context.CommandLong.TargetSystem,
				TargetComponent = context.CommandLong.TargetComponent
			};

			try
			{
				await cancellationSemaphore.WaitAsync(token).ConfigureAwait(false);

				try
				{
					while (!token.IsCancellationRequested)
					{
						await client.SendAsync(cancel);

						await Task.Delay(sequenceTimeout, token);
					}
				}
				finally
				{
					completionSemaphore.Release();
				}
			}
			catch (OperationCanceledException) { }
		}

		private async Task<CommandAck?> RunSequenceAsync(SourceCommandContext context, CancellationToken token)
		{
			byte confirmation = 0;

			do
			{
				if (token.IsCancellationRequested)
				{
					return null;
				}

				using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

				timeoutCancellation.CancelAfter(sequenceTimeout);

				try
				{
					if (!await channel.Reader.WaitToReadAsync(timeoutCancellation.Token).ConfigureAwait(false))
					{
						return null;
					}

					return await channel.Reader.ReadAsync(CancellationToken.None);
				}
				catch (OperationCanceledException)
				{
					if (token.IsCancellationRequested)
					{
						return null;
					}
				}

				if (confirmation == retry)
				{
					return null;
				}

				if (token.IsCancellationRequested)
				{
					return null;
				}

				await client.SendAsync(context.CommandLong with { Confirmation = ++confirmation });
			} while (true);
		}

		private async Task<CommandAck?> RunInProgressAsync()
		{
			do
			{
				using var timeoutCancellation = new CancellationTokenSource();

				timeoutCancellation.CancelAfter(inProgressTimeout);

				try
				{
					if (!await channel.Reader.WaitToReadAsync(timeoutCancellation.Token).ConfigureAwait(false))
					{
						return null;
					}

					var ack = await channel.Reader.ReadAsync(CancellationToken.None);
					if (ack.Result != MavResult.InProgress)
					{
						return ack;
					}
				}
				catch (OperationCanceledException)
				{
					return null;
				}
			} while (true);
		}

		public async Task RunAsync(SourceCommandContext context, CancellationToken token)
		{
			try
			{
				while (channel.Reader.TryRead(out _))
				{
				}

				enabled = true;

				await client.SendAsync(context.CommandLong).ConfigureAwait(false);

				var ack = await RunSequenceAsync(context, token);
				if (ack is null)
				{
					return;
				}

				if (ack.Result == MavResult.InProgress)
				{
					token.Register(() => 
					{
						cancellationSemaphore.Release();
					});

					using var cancellation = new CancellationTokenSource();
					try
					{
						_ = RunCancellationAsync(context, cancellation.Token);

						context.Result = MavResult.InProgress;

						ack = await RunInProgressAsync();
						if (ack is not null)
						{
							context.Result = ack.Result;
						}
					}
					finally
					{
						cancellation.Cancel();
					}
				}
				else
				{
					context.Result = ack.Result;
				}
			}
			finally
			{
				enabled = false;

				completionSemaphore.Release();
			}
		}

		public async Task WaitAsync(CancellationToken token)
		{
			try
			{
				await completionSemaphore.WaitAsync(token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) { }
		}

		public void Dispose()
		{
			completionSemaphore.Dispose();
			cancellationSemaphore.Dispose();
		}
	}
}