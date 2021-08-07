using System.Threading.Tasks;

using Xunit;
using Moq;

using Aerit.MAVLink.Protocols.Command;

namespace Aerit.MAVLink.Tests
{
	public class TargetCommandTests
	{
		[Fact]
		public async Task ProgressTimeOut()
		{
			// Arrange
			var client = new Mock<ICommandClient>();

			using var sut = new TargetCommandProgressHandler(client.Object, MavCmd.DoJump, 1, 42, 1000);

			// Act
			await Task.Delay(1500);

			await sut.AdvanceAsync(50);

			await Task.Delay(1500);

			await sut.CompleteAsync(MavResult.Accepted);

			// Assert
			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o =>
				o.Result == MavResult.InProgress
				&& o.Progress == 0
				&& o.TargetSystem == 1
				&& o.TargetComponent == 42)), Times.Once);

			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o => 
				o.Result == MavResult.InProgress
				&& o.Progress == 50
				&& o.TargetSystem == 1
				&& o.TargetComponent == 42)), Times.Exactly(2));

			client.Verify(o => o.SendAsync(It.Is<CommandAck>(o =>
				o.Result == MavResult.Accepted
				&& o.TargetSystem == 1
				&& o.TargetComponent == 42)), Times.Once);

			client.VerifyNoOtherCalls();
		}
	}
}