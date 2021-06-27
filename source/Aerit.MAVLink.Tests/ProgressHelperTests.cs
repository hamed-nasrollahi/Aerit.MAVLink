using System.Threading.Tasks;

using Xunit;
using Moq;

using Aerit.MAVLink.Protocols.Command;

namespace Aerit.MAVLink.Tests
{
	public class ProgressHelperTests
	{
		[Fact]
		public async Task TimeOut()
		{
			// Arrange
			var client = new Mock<ICommandClient>();

			using var sut = new ProgressHelper(client.Object, 1000);

			// Act
			await Task.Delay(1500);

			await sut.AdvanceAsync(50);

			await Task.Delay(500);

			await sut.CompleteAsync(MavResult.Accepted);

			// Assert
			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o => o.Result == MavResult.InProgress && o.Progress == 0)), Times.Once);
			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o => o.Result == MavResult.InProgress && o.Progress == 50)), Times.Once);
			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o => o.Result == MavResult.Accepted)), Times.Once);
			client.VerifyNoOtherCalls();
		}
	}
}