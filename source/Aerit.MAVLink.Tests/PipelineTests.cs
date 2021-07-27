#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
                .Setup(o => o.ProcessAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<Heartbeat>(), It.IsAny<CancellationToken>()).Result)
                .Callback<byte, byte, Heartbeat, CancellationToken>((systemId, componentId, message, token) => messageOut = message)
                .Returns(true);

            var pipeline = PipelineBuilder
                .Create(NullLoggerFactory.Instance)
                .Append(() => new MatchBufferMiddleware { Target = (1, 42) })
                .Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
                .Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger))
                .Map(map => map
                    .Add(branch => branch
                        .Append<HeartbeatMiddleware>()
                        .Append(() => endpoint.Object))
                )
                .Build();

            var transmissionChannel = new Mock<ITransmissionChannel>();

            transmissionChannel
                .Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((buffer, length) => pipeline.ProcessAsync(buffer.AsMemory(0, length), default));

            using var client = new Client(
                NullLogger<Client>.Instance,
                transmissionChannel.Object,
                Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = 42
				}));

            // Act
            await client.SendAsync(messageIn);

            // Assert
            endpoint.Verify(o => o.ProcessAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<Heartbeat>(), It.IsAny<CancellationToken>()), Times.Once);

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