#nullable enable

using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;
using Moq;

namespace Aerit.MAVLink.Store.Tests
{
	public class StoreTests
	{
		[Fact]
		public async Task WriterReader()
		{
			var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			Directory.CreateDirectory(path);

			try
			{
				// Arrange

				var timestamp = 42L;
				var key = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
				var value = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7 });

				var indexer = new Mock<IIndexer>();

				indexer
					.Setup(o => o.Run(It.IsAny<long>(), It.IsAny<ReadOnlyMemory<byte>>()))
					.Returns(() =>
					{
						var memory = MemoryPool<byte>.Shared.Rent(key.Length);

						key.CopyTo(memory.Memory);

						return (memory, key.Length);
					});

				// Act

				using (var writer = new Store.Writer(NullLogger<Store.Writer>.Instance, path))
				{
					writer.Register(indexer.Object);

					await writer.WriteAsync(timestamp, value);

					await writer.IndexStore.SaveAsync();
				}

				// Assert

				indexer
					.Verify(o => o.Run(It.IsAny<long>(), It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);

				using var reader = new Store.Reader(path);

				var count = 0;
				await foreach (var entry in reader.GetAsync(key))
				{
					try
					{
						Assert.Equal(timestamp, entry.Timestamp);

						Assert.Equal(value.Length, entry.Buffer.Length);

						for (var i = 0; i < value.Length; i++)
						{
							Assert.Equal(value.Span[i], entry.Buffer.Span[i]);
						}
					}
					finally
					{
						entry.Dispose();
					}

					count++;
				}

				Assert.Equal(1, count);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}
	}
}