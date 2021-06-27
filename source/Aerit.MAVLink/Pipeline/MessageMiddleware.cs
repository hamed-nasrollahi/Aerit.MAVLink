#nullable enable

using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink
{
	public interface IMessageMiddleware<T> : IMiddleware
	{
		Task<bool> ProcessAsync(byte systemId, byte componentId, T message);
	}

	public interface IMessageMiddlewareOutput<T>
	{
		IMessageMiddleware<T>? Next { get; set; }
	}

	public class LogMessageEndpoint<T> : IMessageMiddleware<T>
	{
		private readonly ILogger<LogMessageEndpoint<T>> logger;

		public LogMessageEndpoint(ILogger<LogMessageEndpoint<T>> logger)
		{
			this.logger = logger;
		}

		public Task<bool> ProcessAsync(byte systemId, byte componentId, T message)
		{
			logger.LogInformation("{message} from {systemId}/{componentId}", message, systemId, componentId);

			return Task.FromResult(true);
		}
	}
}