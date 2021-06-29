using System.Net;
using System.Threading;

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

var logger = loggerFactory.CreateLogger("Main");

using var transmission = new UdpTransmissionChannel();

await transmission.ConnectAsync(IPEndPoint.Parse("127.0.0.1:3000"));

var client = new Client(loggerFactory.CreateLogger<Client>(), transmission, systemId: 1, componentId: 1);

var pipeline = PipelineBuilder
	.Create(loggerFactory)
	.Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
	.Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger))
	.Map(map => map
		.Add(branch => branch
			.Append(() => new CommandAckMiddleware())
			.Append(() => client.CommandAckEnpoint()))
	)
	.Build();

var commandContext = client.Submit(10, 1, new DoDelivery()
{
	DeliveryMode = 2
});

var cancellation = new CancellationTokenSource();

var listen = client.ListenAsync(pipeline, cancellation.Token);

await commandContext.WaitAsync();

logger.LogInformation("Result: {result}", commandContext.Result);

cancellation.Cancel();

await listen;

var metrics = await MetricsUtils.ExportAsync();

logger.LogInformation(metrics);