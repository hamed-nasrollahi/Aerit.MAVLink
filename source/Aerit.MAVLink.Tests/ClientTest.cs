#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Tests
{
    using Packet = V2.Packet;

    public class ClientTest
    {
        [Fact]
        public async Task PacketHeader()
        {
            // Arrange
            var message = new Heartbeat();

            var transmissionChannel = new Mock<ITransmissionChannel>();

            byte systemId = 0;
            byte componentId = 0;
            byte sequence = 0;

            transmissionChannel
                .Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, CancellationToken>((buffer, length, token) => {
					var packet = Packet.Deserialize(buffer.AsMemory(0, length));
					if (packet is not null)
                    {
                        systemId = packet.SystemId;
                        componentId = packet.ComponentId;
                        sequence = packet.Sequence;
                    }
                });

            using var sut = new Client(transmissionChannel.Object, 1, 42);

            // Act
            for (var i = 0; i < 5; i++)
            {
                await sut.SendAsync(message);
            }

            // Assert
            transmissionChannel
                .Verify(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(5));

            transmissionChannel.VerifyNoOtherCalls();

            Assert.Equal(1, systemId);
            Assert.Equal(42, componentId);
            Assert.Equal(4, sequence);
        }

        [Fact]
        public async Task PacketChecksum()
        {
            // Arrange
            var message = new Heartbeat();

            var transmissionChannel = new Mock<ITransmissionChannel>();

            Packet? packet = null;

            transmissionChannel
                .Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, CancellationToken>((buffer, length, token) => packet = Packet.Deserialize(buffer.AsMemory(0, length)));

            using var sut = new Client(transmissionChannel.Object, 1, 42);

            // Act
            await sut.SendAsync(message);

            // Assert
            transmissionChannel
                .Verify(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            transmissionChannel.VerifyNoOtherCalls();

            Assert.True(packet?.Validate());
        }
    }
}