using System.Threading.Tasks;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Tests
{
    public class CommandTest
    {
        [Fact]
        public async Task MaxRetry()
        {
            // Arrange
            var client = new Mock<ICommandClient>();

            using var handler = new CommandHandler(client.Object);

            var command = new CommandLong { Command = MavCmd.DoWinch };

            var sut = new CommandContext(handler, command);

            // Act

            sut.Submit();

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

            using var handler = new CommandHandler(client.Object);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 1)))
                .Callback<CommandLong>(async o => await handler.EnqueueAsync(new()
                {
                    Command = o.Command,
                    Result = MavResult.Accepted
                }));

            var command = new CommandLong { Command = MavCmd.DoWinch };

            var sut = new CommandContext(handler, command);

            // Act

            sut.Submit();

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

            using var handler = new CommandHandler(client.Object);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    });

                    await Task.Delay(200);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    });

                    await Task.Delay(200);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Accepted
                    });
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };

            var sut = new CommandContext(handler, command);

            // Act

            sut.Submit();

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

            using var handler = new CommandHandler(client.Object);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    });

                    await Task.Delay(2000);

                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Accepted
                    });
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };

            var sut = new CommandContext(handler, command);

            // Act

            sut.Submit();

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

            using var handler = new CommandHandler(client.Object);

            client
                .Setup(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)))
                .Callback<CommandLong>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.InProgress
                    });
                });

            client
                .Setup(o => o.SendAsync(It.IsAny<CommandCancel>()))
                .Callback<CommandCancel>(async o =>
                {
                    await handler.EnqueueAsync(new()
                    {
                        Command = o.Command,
                        Result = MavResult.Cancelled
                    });
                });

            var command = new CommandLong { Command = MavCmd.DoWinch };

            var sut = new CommandContext(handler, command);

            // Act
            sut.Submit();

            await Task.Delay(200);

            sut.Cancel();

            await sut.WaitAsync();

            // Assert
            client.Verify(o => o.SendAsync(It.Is<CommandLong>(cmd => cmd.Confirmation == 0)), Times.Once);
            client.Verify(o => o.SendAsync(It.IsAny<CommandCancel>()), Times.Once);
            client.VerifyNoOtherCalls();

            Assert.Equal(MavResult.Cancelled, sut.Result);
        }
    }
}