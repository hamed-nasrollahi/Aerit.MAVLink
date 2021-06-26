using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FASTER.core;

namespace Aerit.MAVLink.Store
{
	public sealed class IndexStore : IDisposable
	{
		private readonly IDevice device;
		private readonly FasterKV<ReadOnlyMemory<byte>, Memory<byte>> store;

		public IndexStore(string path, bool recover = false)
		{
			device = Devices.CreateLogDevice(Path.Combine(path, "index.log"));

			store = new FasterKV<ReadOnlyMemory<byte>, Memory<byte>>(
				1L << 20,
				new() { LogDevice = device },
				new() { CheckpointDir = path });

			if (recover)
			{
				store.Recover();
			}
		}

		public sealed class Session : IDisposable
		{
			private readonly ClientSession<ReadOnlyMemory<byte>, Memory<byte>, long, (IMemoryOwner<byte> memory, int length), Empty, IndexStoreFunctions> session;

			public Session(ClientSession<ReadOnlyMemory<byte>, Memory<byte>, long, (IMemoryOwner<byte> memory, int length), Empty, IndexStoreFunctions> session)
			{
				this.session = session;
			}

			public async Task<bool> AddAsync(ReadOnlyMemory<byte> key, long address, CancellationToken token = default)
			{
				var result = await session.RMWAsync(ref key, ref address, token: token).ConfigureAwait(false);

				while (result.Status == Status.PENDING)
				{
					result = await result.CompleteAsync(token);
				}

				return result.Status == Status.OK;
			}

			public async Task<Index?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken token = default)
			{
				long input = 0;

				var (status, output) = (await session.ReadAsync(ref key, ref input, cancellationToken: token).ConfigureAwait(false)).Complete();

				if (status != Status.OK)
				{
					return null;
				}

				return new Index(output.memory);
			}

			public void Dispose()
			{
				session.Dispose();
			}
		}

		public Session NewSession()
			=> new(store
				.For(IndexStoreFunctions.Instance)
				.NewSession<IndexStoreFunctions>(
					sessionVariableLengthStructSettings: new() { valueLength = IndexStoreVariableLengthStruct.Instance }));

		public ValueTask<(bool success, Guid token)> SaveAsync(CancellationToken token = default)
			=> store.TakeFullCheckpointAsync(CheckpointType.FoldOver, token);

		public void Dispose()
		{
			store.Dispose();
			device.Dispose();
		}
	}
}