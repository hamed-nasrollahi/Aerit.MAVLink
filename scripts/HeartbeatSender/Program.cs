using System;
using System.Net;
using System.Threading.Tasks;

using Aerit.MAVLink;
using Aerit.MAVLink.Protocols.Connection;

using var transmission = new UdpTransmissionChannel();

try
{
	await transmission.ConnectAsync(IPEndPoint.Parse("127.0.0.1:3000"));
}
catch(Exception ex)
{
    
}

byte systemId = 1;
byte componentId = 1;

var client = new Client(transmission, systemId, componentId);

await using var heartbeat = new HeartbeatBroadcaster(client, 0, MavType.OnboardController, MavAutopilot.Invalid, 0x00);

await heartbeat.UpdateAsync(MavState.Boot);

var pipeline = PipelineBuilder
    .Append(() => new MatchBufferMiddleware { Target = (systemId, componentId) })
    .Append(() => new PacketMiddleware())
    .Append(() => new PacketValidationMiddleware())
    .Append(() => new PacketMapMiddleware()
        .Add(() => BranchBuilder
            .Append(() => new HeartbeatMiddleware())
            .Append(() => new HeartbeatEndpoint())
            .Build())
    )
    .Build();

await heartbeat.UpdateAsync(MavState.Active);

await client.ListenAsync(pipeline);

public class HeartbeatEndpoint : IMessageMiddleware<Heartbeat>
{
	public Task<bool> ProcessAsync(byte systemId, byte componentId, Heartbeat message)
	{
		Console.WriteLine($"{message} from {systemId}/{componentId}");

		return Task.FromResult(true);
	}
}