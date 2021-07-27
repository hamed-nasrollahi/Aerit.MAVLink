using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aerit.MAVLink.Store
{
	public static class Store
	{
		public sealed class Writer : IDisposable
		{
			public class Options
			{
				public string? Path { get; set; }
			}

			private readonly List<IIndexer> indexers = new();

			private readonly ILogger<Writer> logger;

			public Writer(ILogger<Writer> logger, IOptions<Options> options)
			{
				var path = options.Value.Path ?? "./";

				Log = new Log(path);
				IndexStore = new IndexStore(path, recover: false);

				this.logger = logger;
			}

			public Log Log { get; }

			public IndexStore IndexStore { get; }

			public void Register(IIndexer indexer)
			{
				indexers.Add(indexer);
			}

			public async Task<bool> WriteAsync(long timestamp, ReadOnlyMemory<byte> buffer, CancellationToken token = default)
			{
				var length = sizeof(long) + buffer.Length;

				using var entry = MemoryPool<byte>.Shared.Rent(length);

				BitConverter.TryWriteBytes(entry.Memory.Span, timestamp);
				buffer.CopyTo(entry.Memory[sizeof(long)..]);

				var address = await Log.WriteAsync(entry.Memory.Slice(0, length), token).ConfigureAwait(false);
				if (address is null)
				{
					logger.LogError("Unable to write entry");

					return false;
				}

				using var session = IndexStore.NewSession();

				foreach (var indexer in indexers)
				{
					var key = indexer.Run(timestamp, buffer);
					if (key.memory is null)
					{
						continue;
					}

					try
					{
						if (!await session.AddAsync(key.memory.Memory.Slice(0, key.length), address.Value, token))
						{
							logger.LogWarning("Unable to add entry key");
						}
					}
					finally
					{
						key.memory.Dispose();
					}
				}

				return true;
			}

			public void Dispose()
			{
				IndexStore.Dispose();
				Log.Dispose();
			}
		}

		public sealed class Reader : IDisposable
		{
			public class Options
			{
				public string? Path { get; set; }
			}

			public Reader(IOptions<Options> options)
			{
				var path = options.Value.Path ?? "./";

				Log = new Log(path);
				IndexStore = new IndexStore(path, recover: true);
			}

			public Log Log { get; }

			public IndexStore IndexStore { get; }

			public sealed class Entry : IDisposable
			{
				private readonly IMemoryOwner<byte> memory;

				public Entry(IMemoryOwner<byte> memory, int length)
				{
					this.memory = memory;

					Timestamp = BitConverter.ToInt64(memory.Memory.Span);
					Buffer = memory.Memory[sizeof(long)..length];
				}

				public long Timestamp { get; }

				public ReadOnlyMemory<byte> Buffer { get; }

				public void Dispose()
				{
					memory.Dispose();
				}
			}

			public async IAsyncEnumerable<Entry> GetAsync(ReadOnlyMemory<byte> key, [EnumeratorCancellation] CancellationToken token = default)
			{
				using var session = IndexStore.NewSession();

				using var index = await session.GetAsync(key, token);
				if (index is null)
				{
					yield break;
				}

				for (var i = 0; i < index.Count; i++)
				{
					var (memory, length) = await Log.ReadAsync(index[i], sizeof(long) + V2.Packet.MaxLength, token);

					yield return new(memory, length);
				}
			}

			public void Dispose()
			{
				IndexStore.Dispose();
				Log.Dispose();
			}
		}
	}
}