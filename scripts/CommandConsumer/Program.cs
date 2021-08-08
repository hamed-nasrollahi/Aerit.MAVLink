using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Aerit.MAVLink;
using Aerit.MAVLink.Protocols.Command;

using var loggerFactory = LoggerFactory.Create(builder =>
{
	builder
		.SetMinimumLevel(LogLevel.Debug)
		.AddFilter("Microsoft", LogLevel.Warning)
		.AddFilter("System", LogLevel.Warning)
		.AddConsole();
});

var logger = loggerFactory.CreateLogger("Main");

using var transmission = new UdpTransmissionChannelIn(
	Options.Create(new UdpTransmissionChannelIn.Options
	{
		Uri = "0.0.0.0:3000"
	}));

using var client = new Client(
	loggerFactory.CreateLogger<Client>(),
	transmission, Options.Create(new Client.Options
	{
		SystemId = 10,
		ComponentId = 1
	}));

var registry = new TargetCommandHandlerRegistry(client);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.UsePacket()
	.Map(map => map
		.CommandLongEnpoint(async (systemId, componentId, msg, token) =>
		{
			logger.LogInformation("{command} received from {systemId}/{componentId}", msg, systemId, componentId);

			if (!registry.TryAcquire(systemId, componentId, msg.Command, out var handler))
			{
				logger.LogWarning("Unable to acquire ({systemId}, {componentId}, {command})", systemId, componentId, msg.Command);

				return false;
			}

			await client.SendAsync(new CommandAck
			{
				Command = msg.Command,
				Result = MavResult.Accepted,
				TargetSystem = systemId,
				TargetComponent = componentId
			});

			handler.Dispose();

			return true;
		})
		.CommandCancelEnpoint((systemId, componentId, msg) =>
		{
			logger.LogInformation("{cancel} received", msg);

			if (!registry.TryGet(systemId, componentId, msg.Command, out var handler))
			{
				logger.LogWarning("Unable to cancel ({systemId}, {componentId}, {command})", systemId, componentId, msg.Command);

				return false;
			}

			handler.Cancel();

			return true;
		})
	)
	.Build();

await client.ListenAsync(pipeline);