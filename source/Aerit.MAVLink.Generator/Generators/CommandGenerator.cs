using System;
using System.Text;

namespace Aerit.MAVLink.Generator
{
	public static class CommandGenerator
	{
		public static void Run(string ns, EnumDefinition mavCmd, StringBuilder builder)
		{
			/*
            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");
            builder.AppendLine("    public static class CRCExtra");
            builder.AppendLine("    {");
            builder.AppendLine("        public static byte? GetByMessageId(uint messageId) => messageId switch");
            builder.AppendLine("        {");
            */
			foreach (var cmd in mavCmd.Entries)
			{
				if (cmd.Params is null)
				{
					continue;
				}

				foreach (var p in cmd.Params)
				{
					if (p is null)
					{
						continue;
					}

					//Guess type
					if (p.Enum is not null)
					{
						var nullable = p.Default == "NaN";
						if (nullable)
						{

						}
					}
					else if (p.Increment is not null && float.TryParse(p.Increment, out var increment))
					{
						if (increment != Math.Floor(increment))
						{
							throw new NotSupportedException();
						}

						if (p.MinValue is not null && long.TryParse(p.MinValue, out var min))
						{
							if (min >= 0)
							{
								if (p.MaxValue is not null && long.TryParse(p.MaxValue, out var max))
								{
									if (max == 1)
									{
										//bool
									}
									else if (max <= byte.MaxValue)
									{
										//byte
									}
									else if (max <= ushort.MaxValue)
									{
										//ushort
									}
									else
									{
										//uint
									}
								}
								else
								{
									//uint
								}
							}
							else
							{
								if (p.MaxValue is not null && long.TryParse(p.MaxValue, out var max))
								{
									if (min >= sbyte.MinValue && max <= sbyte.MaxValue)
									{
										//sbyte
									}
									else if (min >= short.MinValue && max <= short.MaxValue)
									{
										//short
									}
									else
									{
										//int
									}
								}
								else
								{
									//int
								}
							}
						}
						else
						{
							//int

							//TODO: Validator
							if (p.MaxValue is not null && long.TryParse(p.MaxValue, out var max))
							{
							}
							else
							{
							}
						}

						var nullable = p.Default == "NaN";
						if (nullable)
						{

						}
					}
					else
					{
						//float

						//TODO: Validator min max
					}
				}
			}
		}
		/*
		builder.AppendLine("            _ => null");
		builder.AppendLine("        };");
		builder.AppendLine("    }");
		builder.Append('}');
		*/
	}
}