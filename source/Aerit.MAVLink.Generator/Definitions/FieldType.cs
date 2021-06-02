using System;

namespace Aerit.MAVLink.Generator
{
    public record FieldType(
        string Name,
        byte? Length = null)
    {
        public byte Size => Name switch
        {
            "uint64_t" => 8,
            "int64_t" => 8,
            "double" => 8,
            "uint32_t" => 4,
            "int32_t" => 4,
            "float" => 4,
            "uint16_t" => 2,
            "int16_t" => 2,
            "uint8_t" => 1,
            "int8_t" => 1,
            "char" => 1,
            "uint8_t_mavlink_version" => 1,
            _ => throw new ArgumentException("Invalid type")
        };

        public string CS => Name switch
        {
            "uint64_t" => "ulong",
            "int64_t" => "long",
            "double" => "double",
            "uint32_t" => "uint",
            "int32_t" => "int",
            "float" => "float",
            "uint16_t" => "ushort",
            "int16_t" => "short",
            "uint8_t" => "byte",
            "int8_t" => "sbyte",
            "char" => "string",
            "uint8_t_mavlink_version" => "byte",
            _ => throw new ArgumentException("Invalid type")
        };
    }
}
