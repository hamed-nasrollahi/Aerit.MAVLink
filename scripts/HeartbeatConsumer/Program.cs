using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

using var transmission = new UdpTransmissionChannelIn(
	Options.Create(new UdpTransmissionChannelIn.Options
	{
		Uri = "0.0.0.0:4001"
	}));

using var client = new Client(
	loggerFactory.CreateLogger<Client>(),
	transmission,
	Options.Create(new Client.Options
	{
		SystemId = 10,
		ComponentId = 1
	}));

await using var heartbeat = new HeartbeatBroadcaster(client, 0, MavType.OnboardController, MavAutopilot.Invalid, 0x00);

await heartbeat.UpdateAsync(MavState.Boot);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.UsePacket()
	.Map(map => map
		.Add(branch => branch
			.Append<HeartbeatMiddleware>()
			.Log())
	)
	.Build();

await heartbeat.UpdateAsync(MavState.Active);

await client.ListenAsync(pipeline);