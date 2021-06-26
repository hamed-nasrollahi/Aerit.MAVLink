#nullable enable

using System;

namespace Aerit.MAVLink.V1
{
    public record Packet
    {
		public const int HeaderLength = 6;
		public const int MinLength = 8;
		public const int MaxLength = 263;

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

        public static byte? DeserializeSystemId(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < MinLength)
            {
                return null;
            }

            var span = buffer.Span;

            return span[3];
        }

        public static byte? DeserializeComponentId(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < MinLength)
            {
                return null;
            }

            var span = buffer.Span;

            return span[4];
        }

        public static byte? DeserializeMessageId(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < MinLength)
            {
                return null;
            }

            var span = buffer.Span;

            return span[5];
        }

        public static ReadOnlyMemory<byte>? SlicePayload(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < MinLength)
            {
                return null;
            }

            var span = buffer.Span;

            var length = span[1];

            if (buffer.Length < (MinLength + length))
            {
                return null;
            }

            return buffer.Slice(HeaderLength, length);
        }

        public static Packet? Deserialize(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < MinLength || buffer.Length > MaxLength)
            {
                return null;
            }

            var span = buffer.Span;

            var length = span[1];
            var sequence = span[2];
            var systemId = span[3];
            var componentId = span[4];
            var messageId = span[5];

            if (buffer.Length < (MinLength + length))
            {
                return null;
            }

            var payload = buffer.Slice(HeaderLength, length);

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