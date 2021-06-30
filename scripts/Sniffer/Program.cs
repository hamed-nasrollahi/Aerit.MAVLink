using System.Net;

using Microsoft.Extensions.Logging;

using Aerit.MAVLink;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddConsole();
});

using var transmission = new UdpTransmissionChannel(IPEndPoint.Parse("0.0.0.0:4002"));

using var client = new Client(loggerFactory.CreateLogger<Client>(), transmission, systemId: 255, componentId: 255);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
	.Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger))
	.Map(map => map
		.Add(branch => branch
			.Append(() => new HeartbeatMiddleware())
			.Append((ILogger<LogMessageEndpoint<Heartbeat>> logger) => new LogMessageEndpoint<Heartbeat>(logger)))
	)
	.Build();

await client.ListenAsync(pipeline);