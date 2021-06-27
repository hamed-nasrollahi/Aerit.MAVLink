using System.Net;

using Microsoft.Extensions.Logging;

using Aerit.MAVLink;
using Aerit.MAVLink.Protocols.Connection;

using var loggerFactory = LoggerFactory.Create(builder =>
{
	builder
		.SetMinimumLevel(LogLevel.Debug)
		.AddFilter("Microsoft", LogLevel.Warning)
		.AddFilter("System", LogLevel.Warning)
		.AddConsole();
});

using var transmission = new UdpTransmissionChannel();

await transmission.ConnectAsync(IPEndPoint.Parse("127.0.0.1:3000"));

var client = new Client(loggerFactory.CreateLogger<Client>(), transmission, systemId: 1, componentId: 1);

await using var heartbeat = new HeartbeatBroadcaster(client, 0, MavType.OnboardController, MavAutopilot.Invalid, 0x00);

await heartbeat.UpdateAsync(MavState.Boot);

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

await heartbeat.UpdateAsync(MavState.Active);

await client.ListenAsync(pipeline);