using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Aerit.MAVLink;

using var loggerFactory = LoggerFactory.Create(builder =>
{
	builder
		.SetMinimumLevel(LogLevel.Debug)
		.AddFilter("Microsoft", LogLevel.Warning)
		.AddFilter("System", LogLevel.Warning)
		.AddConsole();
});

var logger = loggerFactory.CreateLogger("Main");

using var transmission = new UdpTransmissionChannelOut(
	Options.Create(new UdpTransmissionChannelOut.Options
	{
		Uri = "127.0.0.1:14552"
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

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.UsePacket()
	.Map(map => map
		.Add(branch => branch
			.Append<CommandAckMiddleware>()
			.Append(() => client.CommandAckEnpoint()))
	)
	.Build();

var cancellation = new CancellationTokenSource();

var listen = client.ListenAsync(pipeline, cancellation.Token);

await client.Submit(new CommandLong
{
	Command = MavCmd.VideoStartStreaming,
	TargetSystem = 1,
	TargetComponent = 101
}).WaitAsync();

await Task.Delay(5000);

await client.SendAsync(new VideoStreamShowAnnotation
{
	Text = "\t\t\t\tRock'n'roll!!!Rock'n'roll!!!Rock'n'roll!!!Rock'n'roll!!!",
	TargetSystem = 1,
	TargetComponent = 101
});

await Task.Delay(5000);

await client.SendAsync(new VideoStreamHideAnnotation
{
	TargetSystem = 1,
	TargetComponent = 101
});

await Task.Delay(5000);

await client.Submit(new CommandLong
{
	Command = MavCmd.VideoStopStreaming,
	TargetSystem = 1,
	TargetComponent = 101
}).WaitAsync();

await Task.Delay(3000);

cancellation.Cancel();

await listen;

var metrics = await MetricsUtils.ExportAsync();

logger.LogInformation(metrics);