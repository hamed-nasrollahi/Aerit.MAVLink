namespace Aerit.MAVLink
{
	using Protocols.Command;

	public record DoDelivery
	{
        public byte DeliveryMode { get; init; }

		public CommandLong ToCommand(byte targetSystem, byte targetComponent) => new()
		{
			TargetSystem = targetSystem,
			TargetComponent = targetComponent,
			Command = MavCmd.DoDelivery,
			Param1 = DeliveryMode
		};
	}

#nullable enable

	public partial class Client
    {
		public SourceCommandContext? Submit(byte targetSystem, byte targetComponent, DoDelivery command)
			=> Submit(command.ToCommand(targetSystem, targetComponent));
	}
}