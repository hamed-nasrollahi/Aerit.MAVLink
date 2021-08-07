using System.Threading.Tasks;

using Xunit;
using Moq;

using Aerit.MAVLink.Protocols.Command;

namespace Aerit.MAVLink.Tests
{
    public class SourceCommandTests
    {
        [Fact]
        public async Task MaxRetry()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 3, 5000);

            var command = new CommandLong { Command = MavCmd.DoWinch };

			// Act
			await using var sut = new SourceCommandContext(handler, command);

			await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 1)), Times.Once);
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 2)), Times.Once);
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 3)), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Null(sut.Result);
        }

        [Fact]
        public async Task RetryAndAck()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 10, 5000);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 1)))
                .Callback<CommandLong>(async o => await handler.EnqueueAsync(new()
                {
                    Command = o.Command,
                    Result = MavResult.Accepted
                }, default));

            var command = new CommandLong { Command = MavCmd.DoWinch };

            // Act

            var sut = new SourceCommandContext(handler, command);

            await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 1)), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.Accepted, sut.Result);
        }

        [Fact]
        public async Task InProgress()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 10, 5000);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    }, default);

                    await Task.Delay(200);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    }, default);

                    await Task.Delay(200);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Accepted
                    }, default);
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };

			// Act
			await using var sut = new SourceCommandContext(handler, command);

			await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.Accepted, sut.Result);
        }

        [Fact]
        public async Task InProgressTimeout()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 10, 1000);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    }, default);

                    await Task.Delay(2000);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Accepted
                    }, default);
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };

			// Act
			await using var sut = new SourceCommandContext(handler, command);

			await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.InProgress, sut.Result);
        }

        [Fact]
        public async Task CancelInProgress()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 10, 5000);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    }, default);
                });

            client
                .Setup(o => o.SendAsync(It.IsAny<CommandCancel>()))
                .Callback<CommandCancel>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Cancelled
                    }, default);
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };
            
            // Act
            await using var sut = new SourceCommandContext(handler, command);

            await Task.Delay(200);

            sut.Cancel();

            await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.Verify(o => o.SendAsync(It.IsAny<CommandCancel>()), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.Cancelled, sut.Result);
        }

        [Fact]
        public async Task RetryCancelInProgress()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new SourceCommandHandler(client.Object, 1000, 10, 5000);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    }, default);
                });

			var sendCancelCount = 0;

			client
                .Setup(o => o.SendAsync(It.IsAny<CommandCancel>()))
                .Callback<CommandCancel>(async o =>
                {
					if (++sendCancelCount == 3)
					{
						await handler.EnqueueAsync(new()
						{
							Command = o.Command,
							Result = MavResult.Cancelled
						}, default);
					}
				});

            var command = new CommandLong { Command = MavCmd.DoWinch };
            
            // Act
            await using var sut = new SourceCommandContext(handler, command);

            await Task.Delay(200);

            sut.Cancel();

            await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.Verify(o => o.SendAsync(It.IsAny<CommandCancel>()), Times.Exactly(3));
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.Cancelled, sut.Result);
        }
    }
}