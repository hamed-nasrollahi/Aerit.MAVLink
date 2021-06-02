namespace Aerit.MAVLink.Generator
{
    public record EntryDefinition(
        string Name,
        string? Description,
        string? Value,
        ParamDefinition?[]? Params,
        DeprecatedDefinition? Deprecated
    );
}
