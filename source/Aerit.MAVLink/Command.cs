#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aerit.MAVLink
{
    public interface ICommandClient
    {
        Task SendAsync(CommandLong message);

        Task SendAsync(CommandCancel message);
    }

    public sealed class CommandHandler : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(0, 1);
        private readonly Channel<CommandAck> channel = Channel.CreateUnbounded<CommandAck>();

        private readonly ICommandClient client;

        public CommandHandler(ICommandClient client)
        {
            this.client = client;
        }

        private int acquired = 0;
        private bool enabled = false;

        public bool TryAcquire()
            => Interlocked.CompareExchange(ref acquired, 1, 0) == 0;

        public void Release()
        {
            acquired = 0;
        }

        public async ValueTask<bool> EnqueueAsync(CommandAck ack)
        {
            if (!enabled)
            {
                return false;
            }

            await channel.Writer.WriteAsync(ack).ConfigureAwait(false);

            return true;
        }

        private async Task<CommandAck?> RunSequenceAsync(CommandContext context, int timeout, byte retry, CancellationToken token)
        {
            byte confirmation = 0;

            do
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

                timeoutCancellation.CancelAfter(timeout);

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

        private async Task<CommandAck?> RunInProgressAsync(int timeout)
        {
            do
            {
                using var timeoutCancellation = new CancellationTokenSource();

                timeoutCancellation.CancelAfter(timeout);

                try
                {
                    if (!await channel.Reader.WaitToReadAsync(timeoutCancellation.Token).ConfigureAwait(false))
                    {
                        return null;
                    }

                    var ack = await channel.Reader.ReadAsync();
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

        public async Task RunAsync(CommandContext context, int sequenceTimeout, byte retry, int inProgressTimeout, CancellationToken token)
        {
            try
            {
                enabled = true;

                token.Register(async () => await client.SendAsync(new CommandCancel()
                {
                    Command = context.CommandLong.Command,
                    TargetSystem = context.CommandLong.TargetSystem,
                    TargetComponent = context.CommandLong.TargetComponent
                }));

                await client.SendAsync(context.CommandLong);

                var ack = await RunSequenceAsync(context, sequenceTimeout, retry, token).ConfigureAwait(false);
                if (ack is null)
                {
                    return;
                }

                context.Result = ack.Result;

                if (context.Result != MavResult.InProgress)
                {
                    return;
                }

                ack = await RunInProgressAsync(inProgressTimeout);
                if (ack is not null)
                {
                    context.Result = ack.Result;
                }
            }
            finally
            {
                enabled = false;
                
                semaphore.Release();
            }
        }

        public async Task WaitAsync(CancellationToken token)
        {
            try
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }

    public sealed class CommandContext : IAsyncDisposable
    {
        private readonly CancellationTokenSource cancellation = new();

        private readonly CommandHandler handler;

        public CommandContext(CommandHandler handler, CommandLong commandLong)
        {
            this.handler = handler;

            CommandLong = commandLong;
        }

        public CommandLong CommandLong { get; }

        public MavResult? Result { get; set; }

        Task? task = null;

        public void Submit()
        {
            task = handler.RunAsync(this, 1000, 3, 1000, cancellation.Token);
        }

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
            if (task is not null)
            {
                await task;
            }

            cancellation.Dispose();
        }
    }

    public sealed class CommandHandlerRegistry
    {
        private readonly ConcurrentDictionary<(byte systemId, byte componentId, MavCmd command), CommandHandler> handlers = new();

        private readonly ICommandClient client;

        public CommandHandlerRegistry(ICommandClient client)
        {
            this.client = client;
        }

        public bool TryGet(byte systemId, byte componentId, MavCmd command, out CommandHandler? handler)
            => handlers.TryGetValue((systemId, componentId, command), out handler);

        public CommandHandler GetOrAdd(byte systemId, byte componentId, MavCmd command)
            => handlers.GetOrAdd((systemId, componentId, command), key => new(client));
    }

    public sealed class CommandAckEnpoint : IMessageMiddleware<CommandAck>
    {
        private readonly CommandHandlerRegistry handlers;

        public CommandAckEnpoint(CommandHandlerRegistry handlers)
        {
            this.handlers = handlers;
        }

        public async Task<bool> ProcessAsync(byte systemId, byte componentId, CommandAck message)
        {
            if (!handlers.TryGet(systemId, componentId, message.Command, out var handler))
            {
                return false;
            }

            return await handler!.EnqueueAsync(message);
        }
    }
}