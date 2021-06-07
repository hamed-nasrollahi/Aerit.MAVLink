using System;
using System.Net.Sockets;
using System.Threading.Tasks;

using Aerit.MAVLink;

using var client = new UdpClient(4001);

while (true)
{
    var frame = await client.ReceiveAsync();

    if (frame.Length > 0)
    {
        if (frame.Buffer[0] == Magic.V2)
        {
            var packet = V2.Packet.Deserialize(frame.Buffer);

            if (packet.Validate(Ping.MessageCRCExtra))
            {
                var ping = Ping.Deserialize(packet.Payload);
            }
        }
    }
}