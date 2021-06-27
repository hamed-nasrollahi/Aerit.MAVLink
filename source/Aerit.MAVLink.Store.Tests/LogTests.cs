#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace Aerit.MAVLink.Store.Tests
{
	public class LogTests
	{
		[Fact]
		public async Task WriteRead()
		{
			// Arrange

			var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			Directory.CreateDirectory(path);

			try
			{
				using var log = new Log(path);

				// Act

				var buffer1 = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
				var buffer2 = new ReadOnlyMemory<byte>(new byte[] { 4, 5, 6 });
				var buffer3 = new ReadOnlyMemory<byte>(new byte[] { 7, 8, 9 });

				var address1 = await log.WriteAsync(buffer1);
				var address2 = await log.WriteAsync(buffer2);
				var address3 = await log.WriteAsync(buffer3);

				// Assert

				async Task assertReadAsync(long address, ReadOnlyMemory<byte> expected)
				{
					var (memory, length) = await log!.ReadAsync(address);
					try
					{
						Assert.Equal(expected.Length, length);

						for (var i = 0; i < length; i++)
						{
							Assert.Equal(expected.Span[i], memory.Memory.Span[i]);
						}
					}
					finally
					{
						memory.Dispose();
					}
				}

				await assertReadAsync(address1.Value, buffer1);
				await assertReadAsync(address2.Value, buffer2);
				await assertReadAsync(address3.Value, buffer3);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[Fact]
		public async Task WriteScan()
		{
			// Arrange

			var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			Directory.CreateDirectory(path);

			try
			{
				using var log = new Log(path);

				// Act

				var buffer1 = new ReadOnlyMemory<byte>(new byte[] { 1 });
				var buffer2 = new ReadOnlyMemory<byte>(new byte[] { 2, 3 });
				var buffer3 = new ReadOnlyMemory<byte>(new byte[] { 4, 5, 6 });

				await log.WriteAsync(buffer1);
				await log.WriteAsync(buffer2);
				await log.WriteAsync(buffer3);

				// Assert

				var count = 0;

				await foreach (var (memory, length) in log.ScanAsync())
				{
					count++;

					Assert.Equal(count, length);

					memory.Dispose();
				}

				Assert.Equal(3, count);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}
	}
}