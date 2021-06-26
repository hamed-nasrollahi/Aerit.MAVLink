using System;
using System.Buffers;

namespace Aerit.MAVLink.Store
{
	public class TargetIndexer : IIndexer
	{
		public static readonly TargetIndexer Instance = new();

		public (IMemoryOwner<byte>? memory, int length) Run(long timestamp, ReadOnlyMemory<byte> buffer)
		{
			uint? messageID = null;
			ReadOnlyMemory<byte>? payload = null;

			switch ((Magic)buffer.Span[0])
			{
				case Magic.V1:
					messageID = V1.Packet.DeserializeMessageId(buffer);
					payload = V1.Packet.SlicePayload(buffer);
					break;

				case Magic.V2:
					messageID = V2.Packet.DeserializeMessageId(buffer);
					payload = V2.Packet.SlicePayload(buffer);
					break;

				default:
					break;
			}

			if (messageID is null || payload is null)
			{
				return default;
			}

			var (targetSystem, targetComponent) = Target.Deserialize(messageID.Value, payload.Value.Span);

			if (targetSystem is null || targetComponent is null)
			{
				return default;
			}

			return Keys.Compute(messageID.Value, targetSystem.Value, targetComponent.Value);
		}
	}
}