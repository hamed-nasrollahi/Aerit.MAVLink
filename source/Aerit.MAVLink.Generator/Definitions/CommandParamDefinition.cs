namespace Aerit.MAVLink.Generator
{
	public record CommandParamDefinition(
		int Index,
		string Type,
		bool Nullable,
		string Label,
		CommandParamValidationDefinition? Validation,
		string? Description,
		string? Units
	);
}