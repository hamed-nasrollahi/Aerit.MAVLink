using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerit.MAVLink.Generator
{
	using static Utils;

	public static class CommandGenerator
	{
		public static string Run(string ns, EntryDefinition cmd, StringBuilder builder)
		{
			var name = CamelCase(cmd.Name, "MAV_CMD");

			var @params = new List<(int index, string type, bool nullable, string label, (long? min, long? max, uint? increment)? validation, string? description, string? units)>();

			if (cmd.Params is not null)
			{
				var index = 0;

				foreach (var param in cmd.Params)
				{
					index++;

					if (param is null || param.Label is null)
					{
						continue;
					}

					var type = string.Empty;
					var label = param.Label.Replace(" ", string.Empty);
					(long? min, long? max, uint? increment)? validation = default;

					if (param.Enum is not null)
					{
						type = CamelCase(param.Enum);
					}
					else if (param.Increment is not null)
					{
						if (!uint.TryParse(param.Increment, out var increment))
						{
							throw new NotSupportedException();
						}

						if (param.MinValue is not null)
						{
							if (!long.TryParse(param.MinValue, out var min))
							{
								throw new NotSupportedException();
							}

							if (min == 0)
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotSupportedException();
									}

									if (max == 1)
									{
										type = "bool";
									}
									else
									{
										if (max < byte.MaxValue)
										{
											type = "byte";

											validation = (null, max, increment);
										}
										else if (max == byte.MaxValue)
										{
											type = "byte";

											if (increment != 1)
											{
												validation = (null, null, increment);
											}
										}
										else if (max < ushort.MaxValue)
										{
											type = "ushort";

											validation = (null, max, increment);
										}
										else if (max == ushort.MaxValue)
										{
											type = "ushort";

											if (increment != 1)
											{
												validation = (null, null, increment);
											}
										}
										else if (max < uint.MaxValue)
										{
											type = "uint";

											validation = (null, max, increment);

										}
										else if (max == uint.MaxValue)
										{
											type = "uint";

											if (increment != 1)
											{
												validation = (null, null, increment);
											}
										}
										else
										{
											throw new NotImplementedException();
										}
									}
								}
								else
								{
									type = "uint";

									validation = (min, null, increment);
								}
							}
							else if (min > 0)
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotImplementedException();
									}

									if (max < byte.MaxValue)
									{
										type = "byte";

										validation = (min, max, increment);
									}
									else if (max == byte.MaxValue)
									{
										type = "byte";

										validation = (min, null, increment);
									}
									else if (max < ushort.MaxValue)
									{
										type = "ushort";

										validation = (min, max, increment);
									}
									else if (max == ushort.MaxValue)
									{
										type = "ushort";

										validation = (min, null, increment);
									}
									else if (max < uint.MaxValue)
									{
										type = "uint";

										validation = (min, max, increment);
									}
									else if (max == uint.MaxValue)
									{
										type = "uint";

										validation = (min, null, increment);
									}
									else
									{
										throw new NotImplementedException();
									}
								}
								else
								{
									type = "uint";

									validation = (min, null, increment);
								}
							}
							else
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotImplementedException();
									}

									if (min >= sbyte.MinValue && max <= sbyte.MaxValue)
									{
										type = "sbyte";

										if (min == sbyte.MinValue)
										{
											if (max == sbyte.MaxValue)
											{
												if (increment != 1)
												{
													validation = (null, null, increment);
												}
											}
											else
											{
												validation = (null, max, increment);
											}
										}
										else
										{
											if (max == sbyte.MaxValue)
											{
												validation = (min, null, increment);
											}
											else
											{
												validation = (min, max, increment);
											}
										}
									}
									else if (min >= short.MinValue && max <= short.MaxValue)
									{
										type = "short";

										if (min == short.MinValue)
										{
											if (max == short.MaxValue)
											{
												if (increment != 1)
												{
													validation = (null, null, increment);
												}
											}
											else
											{
												validation = (null, max, increment);
											}
										}
										else
										{
											if (max == short.MaxValue)
											{
												validation = (min, null, increment);
											}
											else
											{
												validation = (min, max, increment);
											}
										}
									}
									else if (min >= int.MinValue && max <= int.MaxValue)
									{
										type = "int";

										if (min == int.MinValue)
										{
											if (max == int.MaxValue)
											{
												if (increment != 1)
												{
													validation = (null, null, increment);
												}
											}
											else
											{
												validation = (null, max, increment);
											}
										}
										else
										{
											if (max == int.MaxValue)
											{
												validation = (min, null, increment);
											}
											else
											{
												validation = (min, max, increment);
											}
										}
									}
									else
									{
										throw new NotSupportedException();
									}
								}
								else
								{
									type = "int";

									validation = (min, null, increment);
								}
							}
						}
						else
						{
							type = "int";

							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max) || max > int.MaxValue)
								{
									throw new NotImplementedException();
								}

								validation = (null, max, increment);
							}
						}
					}
					else
					{
						type = "float";

						if (param.MinValue is not null)
						{
							if (!long.TryParse(param.MinValue, out var min))
							{
								throw new NotImplementedException();
							}

							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max))
								{
									throw new NotImplementedException();
								}

								validation = (min, max, null);
							}
							else
							{
								validation = (min, null, null);
							}
						}
						else
						{
							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max))
								{
									throw new NotImplementedException();
								}

								validation = (null, max, null);
							}
						}
					}

					@params.Add((index, type, param.Default == "NaN", label, validation, param.Description, param.Units));
				}
			}

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

			builder.AppendLine($"    public record {name}");
			builder.AppendLine("    {");

			builder.AppendLine($"        public const ushort MAVLinkCommand = {cmd.Value};");

			foreach (var (index, type, nullable, label, validation, description, units) in @params)
			{
				builder.AppendLine();

				if (validation is null)
				{
					if (description is not null)
					{
						builder.AppendLine("        /// <summary>");
						foreach (var line in description
							.Split('\n')
							.Select(o => o.TrimStart()))
						{
							builder.AppendLine($"        /// {line}");
						}
						if (units is not null)
						{
							builder.AppendLine($"        /// Units: {units}");

						}
						builder.AppendLine("        /// </summary>");
					}

					builder.AppendLine($@"        public {type}{(nullable ? "?" : "")} {label} {{ get; init; }}");
				}
				else
				{
					var variable = label.Length > 1
						? char.ToLower(label[0]) + label[1..]
						: label.ToLower();

					builder.AppendLine($"        private readonly {type}{(nullable ? "?" : "")} {variable};");
					builder.AppendLine();

					if (description is not null)
					{
						builder.AppendLine("        /// <summary>");
						foreach (var line in description
							.Split('\n')
							.Select(o => o.TrimStart()))
						{
							builder.AppendLine($"        /// {line}");
						}
						if (units is not null)
						{
							builder.AppendLine($"        /// Units: {units}");

						}
						builder.AppendLine("        /// </summary>");
					}

					builder.AppendLine($@"        public {type}{(nullable ? "?" : "")} {label}");
					builder.AppendLine("        {");
					builder.AppendLine($"            get => {variable};");
					builder.AppendLine("            init");
					builder.AppendLine("            {");

					if (validation.Value.min is not null)
					{
						if (nullable)
						{
							builder.AppendLine($"                if (value.HasValue && value < {validation.Value.min})");
						}
						else
						{
							builder.AppendLine($"                if (value < {validation.Value.min})");
						}
						builder.AppendLine("                {");
						builder.AppendLine(@"                    throw new ArgumentException(""Min validation failed"", nameof(value));");
						builder.AppendLine("                }");
					}

					if (validation.Value.max is not null)
					{
						if (nullable)
						{
							builder.AppendLine($"                if (value.HasValue && value > {validation.Value.max})");
						}
						else
						{
							builder.AppendLine($"                if (value > {validation.Value.max})");
						}
						builder.AppendLine("                {");
						builder.AppendLine(@"                    throw new ArgumentException(""Max validation failed"", nameof(value));");
						builder.AppendLine("                }");
					}

					if (validation.Value.increment is not null && validation.Value.increment != 1)
					{
						if (validation.Value.min is not null)
						{
							if (nullable)
							{
								builder.AppendLine($"                if (value.HasValue && ((value - {validation.Value.min}) % {validation.Value.increment}) != 0)");
							}
							else
							{
								builder.AppendLine($"                if (((value - {validation.Value.min}) % {validation.Value.increment}) != 0)");
							}
							builder.AppendLine("                {");
							builder.AppendLine(@"                    throw new ArgumentException(""Increment validation failed"", nameof(value));");
							builder.AppendLine("                }");
						}
						else
						{
							if (nullable)
							{
								builder.AppendLine($"                if (value.HasValue && (value % {validation.Value.increment}) != 0)");
							}
							else
							{
								builder.AppendLine($"                if ((value % {validation.Value.increment}) != 0)");
							}
							builder.AppendLine("                {");
							builder.AppendLine(@"                    throw new ArgumentException(""Increment validation failed"", nameof(value));");
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
					"            TargetSystem = targetSystem",
					"            TargetComponent = targetComponent"
				};

				foreach (var (index, type, nullable, label, _, _, _) in @params)
				{
					if (nullable)
					{
						if (type == "float")
						{
							assignments.Add($"            Param{index} = {label} ?? float.NaN");
						}
						else
							assignments.Add($"            Param{index} = {label}.HasValue ? (float){label}.Value : float.NaN");
						{
						}
					}
					else
					{
						if (type == "float")
						{
							assignments.Add($"            Param{index} = {label}");
						}
						else
						{
							assignments.Add($"            Param{index} = (float){label}");
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

				foreach (var (index, type, nullable, label, _, _, _) in @params)
				{
					if (nullable)
					{
						if (type == "float")
						{
							assignments.Add($"            {label} = float.IsNaN(command.Param{index}) ? (float?)null : command.Param{index}");
						}
						else
						{
							assignments.Add($"            {label} = ({type}?)(float.IsNaN(command.Param{index}) ? (float?)null : command.Param{index})");
						}
					}
					else
					{
						if (type == "float")
						{
							assignments.Add($"            {label} = command.Param{index}");
						}
						else
						{
							assignments.Add($"            {label} = ({type})command.Param{index}");
						}
					}
				}

				builder.AppendLine();
				builder.AppendLine($"        public static {name} FromCommand(CommandLong command) => new()");
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
			builder.AppendLine($"        public SourceCommandContext? Submit(byte targetSystem, byte targetComponent, {name} command)");
			builder.AppendLine("            => Submit(command.ToCommand(targetSystem, targetComponent));");
			builder.AppendLine("    }");
			builder.Append('}');

			return name;
		}
	}
}