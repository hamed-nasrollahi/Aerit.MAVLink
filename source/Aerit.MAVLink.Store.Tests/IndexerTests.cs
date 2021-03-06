#nullable enable

using System.Buffers;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Store.Tests
{
	public class IndexerTests
	{
		[Fact]
		public async Task Source()
		{
			// Arrange
			var message = new Heartbeat();

			var transmissionChannel = new Mock<ITransmissionChannel>();

			(IMemoryOwner<byte>? memory, int length) actual = default;

			transmissionChannel
				.Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((buffer, length) =>
				{
					actual = SourceIndexer.Instance.Run(0, buffer);
				});

			using var sut = new Client(
				NullLogger<Client>.Instance,
				transmissionChannel.Object,
				Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = MavComponent.MavCompIdOnboardComputer
				}));

			// Act
			await sut.SendAsync(message);

			var expected = Keys.Compute(1, (byte)MavComponent.MavCompIdOnboardComputer);

			// Assert
			try
			{
				Assert.NotNull(actual.memory);
				Assert.Equal(expected.length, actual.length);

				for (var i = 0; i < expected.length; i++)
				{
					Assert.Equal(expected.memory.Memory.Span[i], actual.memory!.Memory.Span[i]);
				}
			}
			finally
			{
				expected.memory.Dispose();
				actual.memory?.Dispose();
			}
		}

		[Fact]
		public async Task Target()
		{
			// Arrange
			var message = new CommandLong
			{
				TargetSystem = 2,
				TargetComponent = (byte)MavComponent.MavCompIdOnboardComputer
			};

			var transmissionChannel = new Mock<ITransmissionChannel>();

			(IMemoryOwner<byte>? memory, int length) actual = default;

			transmissionChannel
				.Setup(o => o.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((buffer, length) =>
				{
					actual = TargetIndexer.Instance.Run(0, buffer);
				});

			using var sut = new Client(
				NullLogger<Client>.Instance,
				transmissionChannel.Object,
				Options.Create(new Client.Options
				{
					SystemId = 1,
					ComponentId = MavComponent.MavCompIdOnboardComputer
				}));

			// Act
			await sut.SendAsync(message);

			var expected = Keys.Compute(CommandLong.MAVLinkMessageId, 2, (byte)MavComponent.MavCompIdOnboardComputer);

			// Assert
			try
			{
				Assert.NotNull(actual.memory);
				Assert.Equal(expected.length, actual.length);

				for (var i = 0; i < expected.length; i++)
				{
					Assert.Equal(expected.memory.Memory.Span[i], actual.memory!.Memory.Span[i]);
				}
			}
			finally
			{
				expected.memory.Dispose();
				actual.memory?.Dispose();
			}
		}
	}
}