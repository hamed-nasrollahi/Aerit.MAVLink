namespace Aerit.MAVLink.Generator
{
	public record CommandParamValidationDefinition(
		long? Min,
		long? Max,
		uint? Increment
	);
}