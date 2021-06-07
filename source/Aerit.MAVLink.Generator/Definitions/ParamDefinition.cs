namespace Aerit.MAVLink.Generator
{
    public record ParamDefinition(
        string? Description,
        string? Label,
        string? Units,
        string? Enum,
        string? Increment,
        string? MinValue,
        string? MaxValue,
        string? Default
    );
}
