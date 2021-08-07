using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerit.MAVLink.Generator
{
	public static class CommandGenerator
	{
		public static string Run(string ns, CommandDefinition cmd, StringBuilder builder)
		{
			builder.AppendLine("using System;");
			builder.AppendLine();
			builder.AppendLine($"namespace {ns}");
			builder.AppendLine("{");
			builder.AppendLine("    using Protocols.Command;");
			builder.AppendLine();

			if (cmd.Description is not null)
			{
				builder.AppendLine("    /// <summary>");
				foreach (var line in cmd.Description
					.Split('\n')
					.Select(o => o.TrimStart()))
				{
					builder.AppendLine($"    /// {line}");
				}
				builder.AppendLine("    /// </summary>");
			}

			if (cmd.Deprecated is not null)
			{
				builder.AppendLine($@"    [Obsolete(""{cmd.Deprecated}"")]");
			}

			builder.AppendLine($"    public record {cmd.Name}");
			builder.AppendLine("    {");

			builder.AppendLine($"        public const MavCmd MAVLinkCommand = MavCmd.{cmd.Name};");

			foreach (var param in cmd.Params)
			{
				builder.AppendLine();

				if (param.Validation is null)
				{
					if (param.Description is not null)
					{
						builder.AppendLine("        /// <summary>");
						foreach (var line in param.Description
							.Split('\n')
							.Select(o => o.TrimStart()))
						{
							builder.AppendLine($"        /// {line}");
						}
						if (param.Units is not null)
						{
							builder.AppendLine($"        /// Units: {param.Units}");

						}
						builder.AppendLine("        /// </summary>");
					}

					builder.AppendLine($@"        public {param.Type}{(param.Nullable ? "?" : "")} {param.Label} {{ get; init; }}");
				}
				else
				{
					var variable = param.Label.Length > 1
						? char.ToLower(param.Label[0]) + param.Label[1..]
						: param.Label.ToLower();

					builder.AppendLine($"        private readonly {param.Type}{(param.Nullable ? "?" : "")} {variable};");
					builder.AppendLine();

					if (param.Description is not null)
					{
						builder.AppendLine("        /// <summary>");
						foreach (var line in param.Description
							.Split('\n')
							.Select(o => o.TrimStart()))
						{
							builder.AppendLine($"        /// {line}");
						}
						if (param.Units is not null)
						{
							builder.AppendLine($"        /// Units: {param.Units}");

						}
						builder.AppendLine("        /// </summary>");
					}

					builder.AppendLine($@"        public {param.Type}{(param.Nullable ? "?" : "")} {param.Label}");
					builder.AppendLine("        {");
					builder.AppendLine($"            get => {variable};");
					builder.AppendLine("            init");
					builder.AppendLine("            {");

					if (param.Validation.Min is not null)
					{
						if (param.Nullable)
						{
							builder.AppendLine($"                if (value.HasValue && value < {param.Validation.Min})");
						}
						else
						{
							builder.AppendLine($"                if (value < {param.Validation.Min})");
						}
						builder.AppendLine("                {");
						builder.AppendLine($@"                    throw new ArgumentException(""Min validation failed"", nameof({param.Label}));");
						builder.AppendLine("                }");
					}

					if (param.Validation.Max is not null)
					{
						if (param.Nullable)
						{
							builder.AppendLine($"                if (value.HasValue && value > {param.Validation.Max})");
						}
						else
						{
							builder.AppendLine($"                if (value > {param.Validation.Max})");
						}
						builder.AppendLine("                {");
						builder.AppendLine($@"                    throw new ArgumentException(""Max validation failed"", nameof({param.Label}));");
						builder.AppendLine("                }");
					}

					if (param.Validation.Increment is not null && param.Validation.Increment != 1)
					{
						if (param.Validation.Min is not null)
						{
							if (param.Nullable)
							{
								builder.AppendLine($"                if (value.HasValue && ((value - {param.Validation.Min}) % {param.Validation.Increment}) != 0)");
							}
							else
							{
								builder.AppendLine($"                if (((value - {param.Validation.Min}) % {param.Validation.Increment}) != 0)");
							}
							builder.AppendLine("                {");
							builder.AppendLine($@"                    throw new ArgumentException(""Increment validation failed"", nameof({param.Label}));");
							builder.AppendLine("                }");
						}
						else
						{
							if (param.Nullable)
							{
								builder.AppendLine($"                if (value.HasValue && (value % {param.Validation.Increment}) != 0)");
							}
							else
							{
								builder.AppendLine($"                if ((value % {param.Validation.Increment}) != 0)");
							}
							builder.AppendLine("                {");
							builder.AppendLine($@"                    throw new ArgumentException(""Increment validation failed"", nameof({param.Label}));");
							builder.AppendLine("                }");
						}
					}

					builder.AppendLine($"                {variable} = value;");
					builder.AppendLine("            }");
					builder.AppendLine("        }");
				}
			}

			{
				var assignments = new List<string>
				{
					"            Command = MAVLinkCommand",
					"            TargetSystem = targetSystem",
					"            TargetComponent = targetComponent"
				};

				foreach (var param in cmd.Params)
				{
					if (param.Nullable)
					{
						if (param.Type == "float")
						{
							assignments.Add($"            Param{param.Index} = {param.Label} ?? float.NaN");
						}
						else
							assignments.Add($"            Param{param.Index} = {param.Label}.HasValue ? (float){param.Label}.Value : float.NaN");
						{
						}
					}
					else
					{
						if (param.Type == "float")
						{
							assignments.Add($"            Param{param.Index} = {param.Label}");
						}
						else
						{
							assignments.Add($"            Param{param.Index} = (float){param.Label}");
						}
					}
				}

				builder.AppendLine();
				builder.AppendLine("        public CommandLong ToCommand(byte targetSystem, byte targetComponent) => new()");
				builder.AppendLine("        {");
				builder.AppendLine(string.Join(",\n", assignments));
				builder.AppendLine("        };");
			}

			{
				var assignments = new List<string>();

				foreach (var param in cmd.Params)
				{
					if (param.Nullable)
					{
						if (param.Type == "float")
						{
							assignments.Add($"            {param.Label} = float.IsNaN(command.Param{param.Index}) ? (float?)null : command.Param{param.Index}");
						}
						else
						{
							assignments.Add($"            {param.Label} = ({param.Type}?)(float.IsNaN(command.Param{param.Index}) ? (float?)null : command.Param{param.Index})");
						}
					}
					else
					{
						if (param.Type == "float")
						{
							assignments.Add($"            {param.Label} = command.Param{param.Index}");
						}
						else
						{
							assignments.Add($"            {param.Label} = ({param.Type})command.Param{param.Index}");
						}
					}
				}

				builder.AppendLine();
				builder.AppendLine($"        public static {cmd.Name} FromCommand(CommandLong command) => new()");
				builder.AppendLine("        {");
				builder.AppendLine(string.Join(",\n", assignments));
				builder.AppendLine("        };");
			}

			builder.AppendLine("    }");
			builder.AppendLine();
			builder.AppendLine("#nullable enable");
			builder.AppendLine();
			builder.AppendLine("    public partial class Client");
			builder.AppendLine("    {");
			builder.AppendLine($"        public SourceCommandContext? Submit(byte targetSystem, byte targetComponent, {cmd.Name} command)");
			builder.AppendLine("            => Submit(command.ToCommand(targetSystem, targetComponent));");
			builder.AppendLine("    }");
			builder.Append('}');

			return cmd.Name;
		}
	}
}