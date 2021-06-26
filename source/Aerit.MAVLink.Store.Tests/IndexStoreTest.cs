#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace Aerit.MAVLink.Store.Tests
{
    public class IndexStoreTest
    {
		[Fact]
		public async Task Add()
		{
			// Arrange

			var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			Directory.CreateDirectory(path);

			try
			{
				using var store = new IndexStore(path);

				using var session = store.NewSession();

				// Act

				var key = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

				await session.AddAsync(key, 1);
				await session.AddAsync(key, 2);
				await session.AddAsync(key, 3);

				var index = await session.GetAsync(key);

				// Assert

				Assert.NotNull(index);
				Assert.Equal(3, index!.Count);
				Assert.Equal(1, index![0]);
				Assert.Equal(2, index![1]);
				Assert.Equal(3, index![2]);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}
	}
}