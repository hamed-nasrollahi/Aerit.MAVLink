#nullable enable

using System;

namespace Aerit.MAVLink.V1
{
    public record Packet
    {
        public byte Length { get; init; }

        public byte Sequence { get; init; }

        public byte SystemID { get; init; }

        public byte ComponentID { get; init; }

        public byte MessageID { get; init; }

        public ReadOnlyMemory<byte> Payload { get; init; }

        public ushort Checksum { get; init; }

        public bool Validate(byte messageCRCExtra)
        {
            var crc = Utils.Checksum.Compute(Length);

            crc = Utils.Checksum.Compute(Sequence, crc);
            crc = Utils.Checksum.Compute(SystemID, crc);
            crc = Utils.Checksum.Compute(ComponentID, crc);
            crc = Utils.Checksum.Compute(MessageID, crc);

            foreach (var b in Payload.Span)
            {
                crc = Utils.Checksum.Compute(b, crc);
            }

            crc = Utils.Checksum.Compute(messageCRCExtra, crc);

            return Checksum == crc;
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
            var systemID = span[3];
            var componentID = span[4];
            var messageID = span[5];

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
                SystemID = systemID,
                ComponentID = componentID,
                MessageID = messageID,
                Payload = payload,
                Checksum = checksum
            };
        }
    }
}