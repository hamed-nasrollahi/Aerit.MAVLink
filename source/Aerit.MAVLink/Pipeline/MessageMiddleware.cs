#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink
{
	public interface IMessageMiddleware<T> : IMiddleware
	{
		Task<bool> ProcessAsync(byte systemId, byte componentId, T message, CancellationToken token);
	}

	public interface IMessageMiddlewareOutput<T>
	{
		IMessageMiddleware<T>? Next { get; set; }
	}

	public class MessageEndpoint<T> : IMessageMiddleware<T>
	{
		private readonly Func<byte, byte, T, bool> process;

		public MessageEndpoint(Func<byte, byte, T, bool> process)
		{
			this.process = process;
		}
		
		public IEnumerable<uint>? Ids => null;

		public Task<bool> ProcessAsync(byte systemId, byte componentId, T message, CancellationToken token)
			=> Task.FromResult(process(systemId, componentId, message));
	}

	public class MessageAsyncEndpoint<T> : IMessageMiddleware<T>
	{
		private readonly Func<byte, byte, T, CancellationToken, Task<bool>> process;

		public MessageAsyncEndpoint(Func<byte, byte, T, CancellationToken, Task<bool>> process)
		{
			this.process = process;
		}

		public IEnumerable<uint>? Ids => null;

		public Task<bool> ProcessAsync(byte systemId, byte componentId, T message, CancellationToken token)
			=> process(systemId, componentId, message, token);
	}

	public class LogMessageEndpoint<T> : IMessageMiddleware<T>
	{
		private readonly ILogger<LogMessageEndpoint<T>> logger;

		public LogMessageEndpoint(ILogger<LogMessageEndpoint<T>> logger)
		{
			this.logger = logger;
		}

		public IEnumerable<uint>? Ids => null;

		public Task<bool> ProcessAsync(byte systemId, byte componentId, T message, CancellationToken token)
		{
			logger.LogInformation("{message} from {systemId}/{componentId}", message, systemId, componentId);

			return Task.FromResult(true);
		}
	}
}