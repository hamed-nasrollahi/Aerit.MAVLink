#nullable enable

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Tests
{
	using Packet = V2.Packet;

	public class ClientTests
	{
		[Fact]
		public async Task PacketBufferHeader()
		{
			// Arrange
			var message = new Heartbeat();

			var transmissionChannel = new Mock<ITransmissionChannel>();

			byte systemId = 0;
			byte componentId = 0;
			uint messageId = 0;
			ReadOnlyMemory<byte>? payload = null;

			transmissionChannel
				.Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((buffer, length) =>
				{
                    systemId = Packet.DeserializeSystemId(buffer) ?? 0;
					componentId = Packet.DeserializeComponentId(buffer) ?? 0;
					messageId = Packet.DeserializeMessageId(buffer) ?? 0;
					payload = Packet.SlicePayload(buffer);
				});

			using var sut = new Client(
				NullLogger<Client>.Instance,
				transmissionChannel.Object,
				Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = 42
				}));

			// Act
			await sut.SendAsync(message);

			// Assert
			Assert.Equal(1, systemId);
			Assert.Equal(42, componentId);
			Assert.Equal(Heartbeat.MAVLinkMessageId, messageId);
			Assert.NotNull(payload);
		}

		[Fact]
		public async Task PacketHeader()
		{
			// Arrange
			var message = new Heartbeat();

			var transmissionChannel = new Mock<ITransmissionChannel>();

			byte sequence = 0;
			byte systemId = 0;
			byte componentId = 0;
			uint messageId = 0;

			transmissionChannel
				.Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((buffer, length) =>
				{
					var packet = Packet.Deserialize(buffer.AsMemory(0, length));
					if (packet is not null)
					{
						sequence = packet.Sequence;
						systemId = packet.SystemId;
						componentId = packet.ComponentId;
						messageId = packet.MessageId;
					}
				});

			using var sut = new Client(
				NullLogger<Client>.Instance,
				transmissionChannel.Object,
				Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = 42
				}));

			// Act
			for (var i = 0; i < 5; i++)
			{
				await sut.SendAsync(message);
			}

			// Assert
			transmissionChannel
				.Verify(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Exactly(5));

			transmissionChannel.VerifyNoOtherCalls();

			Assert.Equal(4, sequence);
			Assert.Equal(1, systemId);
			Assert.Equal(42, componentId);
			Assert.Equal(Heartbeat.MAVLinkMessageId, messageId);
		}

		[Fact]
		public async Task PacketChecksum()
		{
			// Arrange
			var message = new Heartbeat();

			var transmissionChannel = new Mock<ITransmissionChannel>();

			Packet? packet = null;

			transmissionChannel
				.Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((buffer, length) => packet = Packet.Deserialize(buffer.AsMemory(0, length)));

			using var sut = new Client(
				NullLogger<Client>.Instance,
				transmissionChannel.Object,
				Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = 42
				}));

			// Act
			await sut.SendAsync(message);

			// Assert
			transmissionChannel
				.Verify(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once);

			transmissionChannel.VerifyNoOtherCalls();

			Assert.True(packet?.Validate());
		}
	}
}