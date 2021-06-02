using System.Collections.Generic;

namespace Aerit.MAVLink.Generator
{
    public record EnumDefinition(
        string Name,
        FieldType? Type,
        bool Bitmask,
        string? Description,
        List<EntryDefinition> Entries,
        DeprecatedDefinition? Deprecated
    );
}
