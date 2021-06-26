using System;
using System.Buffers;

namespace Aerit.MAVLink.Store
{
	public class SourceIndexer : IIndexer
	{
		public static readonly SourceIndexer Instance = new();

		public (IMemoryOwner<byte>? memory, int length) Run(long timestamp, ReadOnlyMemory<byte> buffer)
		{
			byte? systemId = null;
			byte? componentId = null;

			switch ((Magic)buffer.Span[0])
			{
				case Magic.V1:
					systemId = V1.Packet.DeserializeSystemId(buffer);
					componentId = V1.Packet.DeserializeComponentId(buffer);
					break;

				case Magic.V2:
					systemId = V2.Packet.DeserializeSystemId(buffer);
					componentId = V2.Packet.DeserializeComponentId(buffer);
					break;

				default:
					break;
			}

			if (systemId is null || componentId is null)
			{
				return default;
			}

			return Keys.Compute(systemId.Value, componentId.Value);
		}
	}
}