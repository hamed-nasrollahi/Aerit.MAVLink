using System.Threading.Tasks;

using Xunit;
using Moq;

using Aerit.MAVLink.Protocols.Connection;

namespace Aerit.MAVLink.Tests
{
	public class HeartbeatBroadcasterTest
	{
		[Fact]
		public async Task TimeOut()
		{
			// Arrange
			var client = new Mock<IHeartbeatClient>();

			var sut = new HeartbeatBroadcaster(client.Object, 0, MavType.OnboardController, MavAutopilot.Invalid, 0x00);

			// Act
			await Task.Delay(200);

			await sut.UpdateAsync(MavState.Boot);

			await Task.Delay(1200);

			await sut.UpdateAsync(MavState.Active);

			await sut.DisposeAsync();

			// Assert
			client.Verify(o => o.SendAsync(It.Is<Heartbeat>(o => o.SystemStatus == MavState.Boot)), Times.Exactly(2));
			client.Verify(o => o.SendAsync(It.Is<Heartbeat>(o => o.SystemStatus == MavState.Active)), Times.Once);
			client.VerifyNoOtherCalls();
		}
	}
}