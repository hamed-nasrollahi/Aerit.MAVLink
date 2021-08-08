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

using var transmission = new UdpTransmissionChannelOut(
	Options.Create(new UdpTransmissionChannelOut.Options
	{
		Uri = "127.0.0.1:4001"
	}));

await transmission.ConnectAsync();

var client = new Client(
	loggerFactory.CreateLogger<Client>(),
	transmission,
	Options.Create(new Client.Options
	{
		SystemId = 1,
		ComponentId = 1
	}));

await using var heartbeat = new HeartbeatBroadcaster(client, 0, MavType.OnboardController, MavAutopilot.Invalid, 0x00);

await heartbeat.UpdateAsync(MavState.Boot);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.UsePacket()
	.Map(map => map.LogHeartbeat())
	.Build();

await heartbeat.UpdateAsync(MavState.Active);

await client.ListenAsync(pipeline);