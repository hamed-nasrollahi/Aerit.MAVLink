namespace Aerit.MAVLink.Utils
{
    public static class Checksum
    {
        public static ushort Seed { get; } = 0xffff;

        public static ushort Compute(byte data, ushort crc = 0xffff)
        {
            var tmp = (byte)(data ^ (crc & 0xff));

            tmp ^= (byte)(tmp << 4);

            return (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
        }
    }
}