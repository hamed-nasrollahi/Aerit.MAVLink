using System;
using System.Net;
using System.Threading.Tasks;

using Aerit.MAVLink;

using var transmission = new UdpTransmissionChannel(IPEndPoint.Parse("0.0.0.0:4001"));

byte systemId = 10;
byte componentId = 1;

using var client = new Client(transmission, systemId, componentId);

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

await client.ListenAsync(pipeline);

public class PingEndpoint : IMessageMiddleware<Ping>
{
	private readonly Client client;

	public PingEndpoint(Client client)
    {
		this.client = client;
	}

	public async Task<bool> ProcessAsync(byte systemId, byte componentId, Ping message)
	{
		await client.SendAsync(new Ping
   		{
			TimeUsec = message.TimeUsec,
			Seq = message.Seq,
			TargetSystem = systemId,
			TargetComponent = componentId	   
		});

		Console.WriteLine($"{message} from {systemId}/{componentId}");

		return true;
	}
}