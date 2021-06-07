#nullable enable

using System;

namespace Aerit.MAVLink.V2
{
    [Flags]
    public enum IncompatibilityFlags : byte
    {
        Signed = 0x01
    }

    [Flags]
    public enum CompatibilityFlags : byte
    {
    }

    public record Packet
    {
        public byte Length { get; init; }

        public IncompatibilityFlags Incompatibility { get; init; }

        public CompatibilityFlags Compatibility { get; init; }

        public byte Sequence { get; init; }

        public byte SystemID { get; init; }

        public byte ComponentID { get; init; }

        public uint MessageID { get; init; }

        public ReadOnlyMemory<byte> Payload { get; init; }

        public ushort Checksum { get; init; }

        public ReadOnlyMemory<byte>? Signature { get; init; }

        public bool Validate(byte messageCRCExtra)
        {
            var crc = Utils.Checksum.Compute(Length);

            crc = Utils.Checksum.Compute((byte)Incompatibility, crc);
            crc = Utils.Checksum.Compute((byte)Compatibility, crc);
            crc = Utils.Checksum.Compute(Sequence, crc);
            crc = Utils.Checksum.Compute(SystemID, crc);
            crc = Utils.Checksum.Compute(ComponentID, crc);
            crc = Utils.Checksum.Compute((byte)MessageID, crc);
            crc = Utils.Checksum.Compute((byte)(MessageID >> 8), crc);
            crc = Utils.Checksum.Compute((byte)(MessageID >> 16), crc);

            foreach (var b in Payload.Span)
            {
                crc = Utils.Checksum.Compute(b, crc);
            }

            crc = Utils.Checksum.Compute(messageCRCExtra, crc);

            return Checksum == crc;
        }

        public static Packet? Deserialize(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 12 || buffer.Length > 280)
            {
                return null;
            }

            var span = buffer.Span;

            var length = span[1];
            var incompatibility = (IncompatibilityFlags)span[2];
            var compatibility = (CompatibilityFlags)span[3];
            var sequence = span[4];
            var systemID = span[5];
            var componentID = span[6];

            var messageID = span[7]
                | (uint)span[8] << 8
                | (uint)span [9] << 16;

            if (buffer.Length < (12 + length))
            {
                return null;
            }

            var payload = buffer.Slice(10, length);

            var checksum = (ushort)(span[length + 10]
                | span[length + 11] << 8);

            ReadOnlyMemory<byte>? signature = null;
            if ((incompatibility & IncompatibilityFlags.Signed) != 0)
            {
                if (buffer.Length != (12 + length + 13))
                {
                    return null;
                }

                signature = buffer.Slice(length + 12, 13);
            }

            return new()
            {
                Length = length,
                Incompatibility = incompatibility,
                Compatibility = compatibility,
                Sequence = sequence,
                SystemID = systemID,
                ComponentID = componentID,
                MessageID = messageID,
                Payload = payload,
                Checksum = checksum,
                Signature = signature
            };
        }
    }
}