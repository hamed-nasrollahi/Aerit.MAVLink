#nullable enable

using System;

namespace Aerit.MAVLink.V1
{
    public record Packet
    {
        public byte Length { get; init; }

        public byte Sequence { get; init; }

        public byte SystemId { get; init; }

        public byte ComponentId { get; init; }

        public byte MessageId { get; init; }

        public ReadOnlyMemory<byte> Payload { get; init; }

        public ushort Checksum { get; init; }

        public bool Validate()
        {
            var messageCRCExtra = CRCExtra.GetByMessageId(MessageId);
            if (messageCRCExtra is null)
            {
                return false;
            }

            var crc = Utils.Checksum.Compute(Length);

            crc = Utils.Checksum.Compute(Sequence, crc);
            crc = Utils.Checksum.Compute(SystemId, crc);
            crc = Utils.Checksum.Compute(ComponentId, crc);
            crc = Utils.Checksum.Compute(MessageId, crc);

            foreach (var b in Payload.Span)
            {
                crc = Utils.Checksum.Compute(b, crc);
            }

            crc = Utils.Checksum.Compute(messageCRCExtra.Value, crc);

            return Checksum == crc;
        }

        public static byte? DeserializeMessageId(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 6)
            {
                return null;
            }

            var span = buffer.Span;

            return span[5];
        }

        public static Packet? Deserialize(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 8 || buffer.Length > 263)
            {
                return null;
            }

            var span = buffer.Span;

            var length = span[1];
            var sequence = span[2];
            var systemId = span[3];
            var componentId = span[4];
            var messageId = span[5];

            if (buffer.Length < (8 + length))
            {
                return null;
            }

            var payload = buffer.Slice(6, length);

            var checksum = (ushort)(span[length + 6]
                | span[length + 7] << 8);

            return new()
            {
                Length = length,
                Sequence = sequence,
                SystemId = systemId,
                ComponentId = componentId,
                MessageId = messageId,
                Payload = payload,
                Checksum = checksum
            };
        }
    }
}