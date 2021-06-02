namespace Aerit.MAVLink.Generator
{
    public record FieldDefinition(
        byte Index,
        FieldType Type,
        string Name,
        string Description,
        string? Enum,
        string? Units,
        string? Default,
        bool Extension
    );
}
