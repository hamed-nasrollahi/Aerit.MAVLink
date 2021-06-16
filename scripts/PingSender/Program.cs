using System;
using System.Net;
using System.Threading.Tasks;

using Aerit.MAVLink;

using var transmission = new UdpTransmissionChannel(
    IPEndPoint.Parse("0.0.0.0:4002"),
	//IPEndPoint.Parse("127.0.0.1:4001")
	IPEndPoint.Parse("127.0.0.1:3000")
);

var client = new Client(transmission, 11, 1);

var pipeline = PipelineBuilder
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
            TimeUsec = 42,
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