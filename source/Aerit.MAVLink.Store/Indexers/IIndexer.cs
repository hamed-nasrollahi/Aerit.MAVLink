using System;
using System.Buffers;

namespace Aerit.MAVLink.Store
{
	public interface IIndexer
	{
		(IMemoryOwner<byte>? memory, int length) Run(long timestamp, ReadOnlyMemory<byte> buffer);
	}
}