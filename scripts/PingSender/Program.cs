using System;
using System.Net;
using System.Threading.Tasks;

using NodaTime;

using Aerit.MAVLink;

using var transmission = new UdpTransmissionChannel();

await transmission.ConnectAsync(IPEndPoint.Parse("127.0.0.1:3000"));

byte systemId = 100;
byte componentId = 1;

var client = new Client(transmission, systemId, componentId);

var pipeline = PipelineBuilder
    .Append(() => new MatchBufferMiddleware { Target = (systemId, componentId) })
    .Append(() => new PacketMiddleware())
    .Append(() => new PacketValidationMiddleware())
    .Append(() => new PacketMapMiddleware()
        .Add(() => BranchBuilder
            .Append(() => new PingMiddleware())
            .Append(() => new PingEndpoint(client))
            .Build())
    )
    .Build();

await Task.WhenAll(SendPingPeriodicallyAsync(), client.ListenAsync(pipeline));

async Task SendPingPeriodicallyAsync()
{
	uint seq = 0;

	while (true)
    {
		await client.SendAsync(new Ping
		{
            TimeUsec = (ulong)SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
            Seq = seq++,
            TargetSystem = 0,
            TargetComponent = 0
		});

		await Task.Delay(5000);
	}
}

public class PingEndpoint : IMessageMiddleware<Ping>
{
	private readonly Client client;

	public PingEndpoint(Client client)
    {
		this.client = client;
	}

	public async Task<bool> ProcessAsync(byte systemId, byte componentId, Ping message)
	{
		Console.WriteLine($"{message} from {systemId}/{componentId}");

		return true;
	}
}