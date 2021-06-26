using System.Buffers;

namespace Aerit.MAVLink.Store
{
	public static class Keys
	{
		public static (IMemoryOwner<byte> memory, int length) Compute(byte systemId, byte componentId)
		{
			var memory = MemoryPool<byte>.Shared.Rent(2);

			var span = memory.Memory.Span;

			span[0] = systemId;
			span[1] = componentId;

			return (memory, 2);
		}

		public static (IMemoryOwner<byte> memory, int length) Compute(uint messageID, byte? targetSystem, byte? targetComponent)
		{
			var memory = MemoryPool<byte>.Shared.Rent(6);

			var span = memory.Memory.Span;

			span[0] = (byte)messageID;
			span[1] = (byte)(messageID >> 8);
			span[2] = (byte)(messageID >> 16);

			if (targetSystem is null)
			{
				span[3] = 0x00;
				span[4] = 0x00;
			}
			else
			{
				span[3] = 0x01;
				span[4] = targetSystem.Value;
			}

			if (targetComponent is null)
			{
				span[5] = 0x00;
			}
			else
			{
				span[3] |= 0x02;
				span[5] = targetComponent.Value;
			}

			return (memory, 6);
		}
	}
}