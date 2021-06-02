using System.Collections.Generic;

namespace Aerit.MAVLink.Generator
{
    public record MessageDefinition(
        uint ID,
        string Name,
        string? Description,
        List<FieldDefinition> Fields,
        byte BaseLength,
        byte Length,
        byte CRC,
        DeprecatedDefinition? Deprecated
    );
}
