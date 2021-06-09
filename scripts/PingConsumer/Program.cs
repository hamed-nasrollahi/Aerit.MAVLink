using System.Net;
using System.Net.Sockets;

using Aerit.MAVLink;

using PacketV1 = Aerit.MAVLink.V1.Packet;
using PacketV2 = Aerit.MAVLink.V2.Packet;

using var client = new UdpClient(4001);

/*
while (true)
{
    var frame = await client.ReceiveAsync();

    if (frame.Buffer.Length > 0)
    {
        switch ((Magic)frame.Buffer[0])
        {
            case Magic.V1:
                {
                    var packet = PacketV1.Deserialize(frame.Buffer);

                    if (packet.Validate())
                    {
                        var ping = Ping.Deserialize(packet.Payload.Span);
                    }
                }
                break;

            case Magic.V2:
                {
                    var packet = PacketV2.Deserialize(frame.Buffer);

                    if (packet.Validate())
                    {
                        var ping = Ping.Deserialize(packet.Payload.Span);
                    }
                }
                break;

            default:
                break;
        }
    }
}
*/