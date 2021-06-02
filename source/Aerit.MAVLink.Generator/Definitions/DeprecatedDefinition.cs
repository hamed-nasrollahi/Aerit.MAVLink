namespace Aerit.MAVLink.Generator
{
    public record DeprecatedDefinition(
        string? Description,
        string? Since,
        string? ReplacedBy
    );
}
