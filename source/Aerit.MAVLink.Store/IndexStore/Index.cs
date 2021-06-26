using System;
using System.Buffers;

namespace Aerit.MAVLink.Store
{
	public sealed class Index : IDisposable
	{
		private readonly IMemoryOwner<byte> memory;

		public Index(IMemoryOwner<byte> memory)
		{
			this.memory = memory;
		}

		public int Count => BitConverter.ToInt32(memory.Memory.Span);

		public long this[int index] => BitConverter.ToInt64(memory.Memory.Span[(sizeof(int) + index * sizeof(long))..]);

		public void Dispose()
		{
			memory.Dispose();
		}
	}
}