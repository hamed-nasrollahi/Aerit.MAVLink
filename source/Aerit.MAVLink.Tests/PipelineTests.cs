#nullable enable

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Tests
{
    public class PipelineTests
    {
        [Fact]
        public async Task HeartbeatEndpointRouting()
        {
            // Arrange
            var messageIn = new Heartbeat()
            {
                CustomMode = 0x42000000,
                Type = (MavType)0x46,
                Autopilot = (MavAutopilot)0x47,
                BaseMode = (MavModeFlag)0x48,
                SystemStatus = (MavState)0x49,
                MavlinkVersion = 0xfd
            };

            Heartbeat? messageOut = null;

            var endpoint = new Mock<IMessageMiddleware<Heartbeat>>();

            endpoint
                .Setup(o => o.ProcessAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<Heartbeat>()).Result)
                .Callback<byte, byte, Heartbeat>((systemId, componentId, message) => messageOut = message)
                .Returns(true);

            var pipeline = PipelineBuilder
                .Create(NullLoggerFactory.Instance)
                .Append(() => new MatchBufferMiddleware { Target = (1, 42) })
                .Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
                .Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger))
                .Map(map => map
                    .Add(branch => branch
                        .Append(() => new HeartbeatMiddleware())
                        .Append(() => endpoint.Object))
                )
                .Build();

            var transmissionChannel = new Mock<ITransmissionChannel>();

            transmissionChannel
                .Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((buffer, length) => pipeline.ProcessAsync(buffer.AsMemory(0, length)));

            using var client = new Client(NullLogger<Client>.Instance, transmissionChannel.Object, 1, 42);

            // Act
            await client.SendAsync(messageIn);

            // Assert
            endpoint.Verify(o => o.ProcessAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<Heartbeat>()), Times.Once);

            Assert.NotNull(messageOut);

            Assert.Equal(messageIn.CustomMode, messageOut!.CustomMode);
            Assert.Equal(messageIn.Type, messageOut!.Type);
            Assert.Equal(messageIn.Autopilot, messageOut!.Autopilot);
            Assert.Equal(messageIn.BaseMode, messageOut!.BaseMode);
            Assert.Equal(messageIn.SystemStatus, messageOut!.SystemStatus);
            Assert.Equal(messageIn.MavlinkVersion, messageOut!.MavlinkVersion);
        }
    }
}