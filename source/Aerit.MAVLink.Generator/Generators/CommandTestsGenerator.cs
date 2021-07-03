using System.Collections.Generic;
using System.Text;

namespace Aerit.MAVLink.Generator
{
	using static Utils;

	public static class CommandTestsGenerator
	{
		private static string ParamTestValue(CommandParamDefinition param)
		{
			var nullable = param.Nullable ? "?" : string.Empty;

			switch (param.Type)
			{
				case "bool":
					return "true";

				case "float":
					if (param.Validation is null)
					{
						return "0.42f";
					}
                    else
                    {
                        return $"{(param.Validation.Min ?? 0.0) + 0.42}f";
                    }

				case "byte":
				case "ushort":
				case "uint":
					if (param.Validation is null)
					{
						return "0x42";
					}
					else
					{
						return $"{(param.Validation.Min ?? 0) + (param.Validation.Increment ?? 1)}";
					}

				case "sbyte":
				case "short":
				case "int":
					if (param.Validation is null)
					{
						return "0x42";
					}
					else
					{
						if (param.Validation.Max.HasValue)
						{
							return $"{param.Validation.Max.Value - (param.Validation.Increment ?? 1)}";
						}
						else
						{
							return $"{(param.Validation.Min ?? 0) + (param.Validation.Increment ?? 1)}";
						}
					}

				default:
					return $"({param.Type}{nullable})0x42";
			}
		}

		public static string Run(string ns, CommandDefinition cmd, StringBuilder builder)
		{
			var assignments = new List<string>();

			foreach (var param in cmd.Params)
			{
				assignments.Add($"                {param.Label} = {ParamTestValue(param)}");
			}

			builder.AppendLine("using Xunit;");
			builder.AppendLine();

			builder.AppendLine($"namespace {ns}.Tests");
			builder.AppendLine("{");
			builder.AppendLine($"    public class {cmd.Name}Tests");
			builder.AppendLine("    {");
			builder.AppendLine("        [Fact]");
			builder.AppendLine("        public void Roundtrip()");
			builder.AppendLine("        {");
			builder.AppendLine("            // Arrange");
			builder.AppendLine($"            var commandIn = new {cmd.Name}()");
			builder.AppendLine("            {");
			builder.AppendLine(string.Join(",\n", assignments));
			builder.AppendLine("            };");

			builder.AppendLine();

			builder.AppendLine("            // Act");
			builder.AppendLine("            var commandLong = commandIn.ToCommand(1, 1);");
			builder.AppendLine($"            var commandOut = {cmd.Name}.FromCommand(commandLong);");

			builder.AppendLine();

			builder.AppendLine("            // Assert");
			foreach (var param in cmd.Params)
			{
				builder.AppendLine($"            Assert.Equal(commandIn.{param.Label}, commandOut.{param.Label});");
			}
			builder.AppendLine("        }");

			builder.AppendLine("    }");
			builder.Append('}');

			return cmd.Name;
		}
	}
}