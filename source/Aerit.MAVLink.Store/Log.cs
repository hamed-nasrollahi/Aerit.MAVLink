using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using FASTER.core;

namespace Aerit.MAVLink.Store
{
	public sealed class Log : IDisposable
	{
		private readonly IDevice device;
		private readonly FasterLog log;

		public Log(string path)
		{
			device = Devices.CreateLogDevice(Path.Combine(path, "messages.log"));
			log = new FasterLog(new FasterLogSettings { LogDevice = device });
		}

		public async ValueTask<long?> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
		{
			var address = await log.EnqueueAsync(buffer, token).ConfigureAwait(false);

			await log.CommitAsync(token);

			return address;
		}

		public async Task<(IMemoryOwner<byte> memory, int length)> ReadAsync(long address, int estimatedLength = 0, CancellationToken token = default)
		{
			var (bytes, length) = await log.ReadAsync(address, estimatedLength, token).ConfigureAwait(false);

			var memory = MemoryPool<byte>.Shared.Rent(length);

			bytes.CopyTo(memory.Memory);

			return (memory, length);
		}

		public async IAsyncEnumerable<(IMemoryOwner<byte> memory, int length)> ScanAsync(long? begin = null, long? end = null, [EnumeratorCancellation] CancellationToken token = default)
		{
			var iter = log.Scan(begin ?? log.BeginAddress, end ?? log.TailAddress);

			while (true)
			{
				token.ThrowIfCancellationRequested();

				IMemoryOwner<byte> entry;
				int length;
				long currentAddress;
				long nextAddress;

				while (!iter.GetNext(MemoryPool<byte>.Shared, out entry, out length, out currentAddress, out nextAddress))
				{
					if (currentAddress == nextAddress)
					{
						break;
					}

					await iter.WaitAsync(token).ConfigureAwait(false);
				}

				if (currentAddress == nextAddress)
				{
					break;
				}

				yield return (entry, length);
			}
		}

		public void Dispose()
		{
			log.Dispose();
			device.Dispose();
		}
	}
}