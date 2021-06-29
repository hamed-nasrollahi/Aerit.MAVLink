using System.Net;

using Microsoft.Extensions.Logging;

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

using var transmission = new UdpTransmissionChannel(IPEndPoint.Parse("0.0.0.0:3000"));

using var client = new Client(loggerFactory.CreateLogger<Client>(), transmission, systemId: 10, componentId: 1);

var registry = new TargetCommandHandlerRegistry(client);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
	.Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger))
	.Map(map => map
		.Add(branch => branch
			.Append(() => new CommandLongMiddleware())
            .Enpoint(async (systemId, componentId, msg, token) => 
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
			}))
		.Add(branch => branch
			.Append(() => new CommandCancelMiddleware())
            .Enpoint((systemId, componentId, msg) => 
			{
				logger.LogInformation("{cancel} received", msg);

				if (!registry.TryGet(systemId, componentId, msg.Command, out var handler))
				{
					logger.LogWarning("Unable to cancel ({systemId}, {componentId}, {command})", systemId, componentId, msg.Command);

					return false;
				}

				handler.Cancel();

				return true;
			}))
	)
	.Build();

await client.ListenAsync(pipeline);