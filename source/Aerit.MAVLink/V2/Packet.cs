#nullable enable

using System;
using System.Security.Cryptography;

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

    public record Signature
    {
        public byte LinkId { get; init; }

        public ulong TimeStamp48 { get; init; }

        public ulong Signature48 { get; init; }

        public static Signature Deserialize(ReadOnlySpan<byte> buffer)
        {
            var linkId = buffer[0];

            var timeStamp48 = buffer[1]
                | (ulong)buffer[2] << 8
                | (ulong)buffer[3] << 16
                | (ulong)buffer[4] << 24
                | (ulong)buffer[5] << 32
                | (ulong)buffer[6] << 40;

            var signature48 = buffer[7]
                | (ulong)buffer[8] << 8
                | (ulong)buffer[9] << 16
                | (ulong)buffer[10] << 24
                | (ulong)buffer[11] << 32
                | (ulong)buffer[12] << 40;

            return new()
            {
                LinkId = linkId,
                TimeStamp48 = timeStamp48,
                Signature48 = signature48
            };
        }

        public static ulong Compute(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
        {
            Span<byte> buffer = stackalloc byte[32 + data.Length];
            Span<byte> destination = stackalloc byte[32];

            key.CopyTo(buffer);
            data.CopyTo(buffer[32..]);

            using var sha256 = SHA256.Create();

            sha256.TryComputeHash(buffer, destination, out _);

            return destination[0]
                | (ulong)destination[1] << 8
                | (ulong)destination[2] << 16
                | (ulong)destination[3] << 24
                | (ulong)destination[4] << 32
                | (ulong)destination[5] << 40;
        }
    }

    public record Packet
    {
        public byte Length { get; init; }

        public IncompatibilityFlags Incompatibility { get; init; }

        public CompatibilityFlags Compatibility { get; init; }

        public byte Sequence { get; init; }

        public byte SystemId { get; init; }

        public byte ComponentId { get; init; }

        public uint MessageId { get; init; }

        public ReadOnlyMemory<byte> Payload { get; init; }

        public ushort Checksum { get; init; }

        public Signature? Signature { get; init; }

        public bool Validate()
        {
            var messageCRCExtra = CRCExtra.GetByMessageId(MessageId);
            if (messageCRCExtra is null)
            {
                return false;
            }

            var crc = Utils.Checksum.Compute(Length);

            crc = Utils.Checksum.Compute((byte)Incompatibility, crc);
            crc = Utils.Checksum.Compute((byte)Compatibility, crc);
            crc = Utils.Checksum.Compute(Sequence, crc);
            crc = Utils.Checksum.Compute(SystemId, crc);
            crc = Utils.Checksum.Compute(ComponentId, crc);
            crc = Utils.Checksum.Compute((byte)MessageId, crc);
            crc = Utils.Checksum.Compute((byte)(MessageId >> 8), crc);
            crc = Utils.Checksum.Compute((byte)(MessageId >> 16), crc);

            foreach (var b in Payload.Span)
            {
                crc = Utils.Checksum.Compute(b, crc);
            }

            crc = Utils.Checksum.Compute(messageCRCExtra.Value, crc);

            return Checksum == crc;
        }

        public static uint? DeserializeMessageId(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 10)
            {
                return null;
            }

            var span = buffer.Span;

            return span[7]
                | (uint)span[8] << 8
                | (uint)span [9] << 16;
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
            var systemId = span[5];
            var componentId = span[6];

            var messageId = span[7]
                | (uint)span[8] << 8
                | (uint)span [9] << 16;

            if (buffer.Length < (12 + length))
            {
                return null;
            }

            var payload = buffer.Slice(10, length);

            var checksum = (ushort)(span[length + 10]
                | span[length + 11] << 8);

            Signature? signature = null;
            if ((incompatibility & IncompatibilityFlags.Signed) != 0)
            {
                if (buffer.Length != (12 + length + 13))
                {
                    return null;
                }

                signature = Signature.Deserialize(span.Slice(length + 12, 13));
            }

            return new()
            {
                Length = length,
                Incompatibility = incompatibility,
                Compatibility = compatibility,
                Sequence = sequence,
                SystemId = systemId,
                ComponentId = componentId,
                MessageId = messageId,
                Payload = payload,
                Checksum = checksum,
                Signature = signature
            };
        }
    }
}