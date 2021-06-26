using System;
using System.Buffers;

using FASTER.core;

namespace Aerit.MAVLink.Store
{
	public class IndexStoreFunctions : FunctionsBase<ReadOnlyMemory<byte>, Memory<byte>, long, (IMemoryOwner<byte> memory, int length), Empty>
	{
		public static readonly IndexStoreFunctions Instance = new();

		public IndexStoreFunctions(bool locking = false)
			: base(locking)
		{
		}

		public override void SingleWriter(ref ReadOnlyMemory<byte> key, ref Memory<byte> src, ref Memory<byte> dst)
		{
			src.CopyTo(dst);
		}

		public override bool ConcurrentWriter(ref ReadOnlyMemory<byte> key, ref Memory<byte> src, ref Memory<byte> dst)
		{
			if (dst.Length < src.Length || dst.IsMarkedReadOnly())
			{
				dst.MarkReadOnly();
				return false;
			}

			src.CopyTo(dst);

			dst.ShrinkSerializedLength(src.Length);

			return true;
		}

		public override void SingleReader(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> value, ref (IMemoryOwner<byte> memory, int length) dst)
		{
			dst = (MemoryPool<byte>.Shared.Rent(value.Length), value.Length);
			value.CopyTo(dst.memory.Memory);
		}

		public override void ConcurrentReader(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> value, ref (IMemoryOwner<byte> memory, int length) dst)
		{
			dst = (MemoryPool<byte>.Shared.Rent(value.Length), value.Length);
			value.CopyTo(dst.memory.Memory);
		}

		public override void InitialUpdater(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> value)
		{
			BitConverter.TryWriteBytes(value.Span, 1);
			BitConverter.TryWriteBytes(value.Span[sizeof(int)..], input);
		}

		public override void CopyUpdater(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> oldValue, ref Memory<byte> newValue)
		{
			BitConverter.TryWriteBytes(newValue.Span, BitConverter.ToInt32(oldValue.Span) + 1);
			oldValue[sizeof(int)..].CopyTo(newValue[sizeof(int)..]);
			BitConverter.TryWriteBytes(newValue.Span[^sizeof(long)..], input);
		}

		public override bool InPlaceUpdater(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> value)
			=> false;

		public override bool NeedCopyUpdate(ref ReadOnlyMemory<byte> key, ref long input, ref Memory<byte> oldValue)
			=> true;

		public override bool SupportsLocking => locking;

		public override void Lock(ref RecordInfo recordInfo, ref ReadOnlyMemory<byte> key, ref Memory<byte> value, LockType lockType, ref long lockContext)
		{
			value.SpinLock();
		}

		public override bool Unlock(ref RecordInfo recordInfo, ref ReadOnlyMemory<byte> key, ref Memory<byte> value, LockType lockType, long lockContext)
		{
			value.Unlock();
			return true;
		}
	}
}