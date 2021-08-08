using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink.Services.Logger
{
	using Store;

	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> logger;
		private readonly Client client;
		private readonly PipelineBuilder<IBufferMiddleware> pipelineBuilder;
		private readonly Store.Writer writer;

		public Worker(ILogger<Worker> logger, Client client, PipelineBuilder<IBufferMiddleware> pipelineBuilder, Store.Writer writer)
		{
			this.logger = logger;
			this.client = client;
			this.pipelineBuilder = pipelineBuilder;
			this.writer = writer;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			logger.LogInformation("Started");

			var count = 0;

			try
			{
				var pipeline = pipelineBuilder
					.Endpoint(async (buffer, context, token) =>
					{
						await writer.WriteAsync(client.TimeBootMs(), buffer, token);

						count++;

						return true;
					})
					.Build();

				await client.ListenAsync(pipeline, stoppingToken);
			}
			catch (OperationCanceledException)
            {
            }
			catch (Exception exception)
			{
				logger.LogError(exception, "Exception caught");
			}
			finally
			{
				try
				{
					await writer.IndexStore.SaveAsync(CancellationToken.None);
				}
				catch (Exception exception)
				{
					logger.LogError(exception, "Exception caught");
				}
			}

			logger.LogInformation("{count} messages captured", count);

			logger.LogInformation("Done");
		}
	}
}