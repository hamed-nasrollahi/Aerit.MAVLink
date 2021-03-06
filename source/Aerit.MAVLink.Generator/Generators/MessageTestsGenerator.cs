using System;
using System.Collections.Generic;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class MessageTestsGenerator
    {
        private static string BaseTypeTestValue(FieldType type, int offset, int index)
            => type.Name switch
            {
                "uint64_t" or "int64_t" => $"0x{0x42000000000000 | (long)offset << 16 | (long)index:x16}",
                "double" => $"{42.0 + (offset / 255.0) + index}",
                "uint32_t" or "int32_t" => $"0x{0x42000000 | offset << 16 | index:x8}",
                "float" => $"{42.0f + (offset / 255.0f) + index}f",
                "uint16_t" or "int16_t" => $"0x{0x4200 + offset + index:x4}",
                "uint8_t" => $"0x{(0x42 + offset + index) & 0xff:x2}",
                "int8_t" => $"0x{1 + index:x2}",
                _ => throw new Exception("Unexpected Type")
            };

        private static string FieldTestValue(FieldDefinition field, int offset)
        {
            if (field.Type.Name == "char")
            {
                return "\"" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, Math.Min((byte)26, field.Type.Length ?? 0)) + "\"";
            }
            else if (field.Type.Length is not null)
            {
                var values = new List<string>();

                if (field.Enum is not null)
                {
                    var enumName = CamelCase(field.Enum);

                    for (var i = 0; i < field.Type.Length; i++)
                    {
                        values.Add($"({enumName}){BaseTypeTestValue(field.Type, offset, i)}");
                    }

                    return $@"new[] {{ {string.Join(", ", values)} }}";
                }
                else
                {
                    for (var i = 0; i < field.Type.Length; i++)
                    {
                        values.Add(BaseTypeTestValue(field.Type, offset, i));
                    }

                    return $@"new {field.Type.CS}[] {{ {string.Join(", ", values)} }}";
                }
            }
            else
            {
                return field.Enum is not null
                    ? $"({CamelCase(field.Enum)}){BaseTypeTestValue(field.Type, offset, 0)}"
                    : BaseTypeTestValue(field.Type, offset, 0);
            }
        }

        public static string Run(string ns, MessageDefinition message, StringBuilder builder)
        {
            var name = CamelCase(message.Name);

            builder.AppendLine("using System;");
            builder.AppendLine();
            builder.AppendLine("using Xunit;");
            builder.AppendLine();
            builder.AppendLine($"namespace {ns}.Tests");
            builder.AppendLine("{");
            builder.AppendLine($"    public class {name}Tests");
            builder.AppendLine("    {");
            builder.AppendLine("        [Fact]");
            builder.AppendLine("        public void RoundtripSerialization()");
            builder.AppendLine("        {");
            builder.AppendLine("            // Arrange");
            builder.AppendLine($"            Span<byte> buffer = new byte[{name}.MAVLinkMessageLength];");
            builder.AppendLine($"            var messageIn = new {name}()");
            builder.AppendLine("            {");

            var fields = new List<string>();
            var assignments = new List<string>();

            var offset = 0;

			string? targetSystem = null;
			string? targetComponent = null;

			foreach (var field in message.Fields)
            {
                var fieldName = CamelCase(field.Name);
                if (fieldName == name)
                {
                    fieldName += "_";
                }

                fields.Add(fieldName);

				var value = FieldTestValue(field, offset);

				assignments.Add($"                {fieldName} = {value}");

                offset += field.Type.Size * (field.Type.Length ?? 1);

                if (field.Name == "target_system")
                {
					targetSystem = value;
				}
                else if (field.Name == "target_component")
                {
					targetComponent = value;
                }
            }

            builder.AppendLine(string.Join(",\n", assignments));
            builder.AppendLine("            };");
            builder.AppendLine();
            builder.AppendLine("            // Act");
            builder.AppendLine("            messageIn.Serialize(buffer);");
            builder.AppendLine($"            var messageOut = {name}.Deserialize(buffer);");
            builder.AppendLine();
            builder.AppendLine("            // Assert");
            foreach (var field in fields)
            {
                builder.AppendLine($"            Assert.Equal(messageIn.{field}, messageOut.{field});");
            }
            builder.AppendLine("        }");

			builder.AppendLine();

			builder.AppendLine("        [Fact]");
            builder.AppendLine("        public void DeserializeTarget()");
            builder.AppendLine("        {");
            builder.AppendLine("            // Arrange");
            builder.AppendLine($"            Span<byte> buffer = new byte[{name}.MAVLinkMessageLength];");
            builder.AppendLine($"            var message = new {name}()");
            builder.AppendLine("            {");
            builder.AppendLine(string.Join(",\n", assignments));
            builder.AppendLine("            };");
            builder.AppendLine();
            builder.AppendLine("            // Act");
            builder.AppendLine("            message.Serialize(buffer);");
            builder.AppendLine($"            var (targetSystemOut, targetComponentOut) = {name}.DeserializeTarget(buffer);");
            builder.AppendLine();
            builder.AppendLine("            // Assert");
            builder.AppendLine($"            Assert.Equal((byte?){targetSystem ?? "null"}, targetSystemOut);");
            builder.AppendLine($"            Assert.Equal((byte?){targetComponent ?? "null"}, targetComponentOut);");
            builder.AppendLine("        }");

			builder.AppendLine();

			builder.AppendLine("        [Theory]");
			if (targetSystem is not null)
			{
				builder.AppendLine($"        [InlineData({targetSystem}, {targetComponent ?? "null"}, true)]");
                builder.AppendLine("        [InlineData(0, 0, false)]");
			}
            else
            {
                builder.AppendLine("        [InlineData(0, 0, true)]");
            }
			builder.AppendLine("        [InlineData(null, null, true)]");
            builder.AppendLine("        public void MatchTarget(int? targetSystem, int? targetComponent, bool expected)");
            builder.AppendLine("        {");
            builder.AppendLine("            // Arrange");
            builder.AppendLine($"            Span<byte> buffer = new byte[{name}.MAVLinkMessageLength];");
            builder.AppendLine($"            var message = new {name}()");
            builder.AppendLine("            {");
            builder.AppendLine(string.Join(",\n", assignments));
            builder.AppendLine("            };");
            builder.AppendLine();
            builder.AppendLine("            // Act");
            builder.AppendLine("            message.Serialize(buffer);");
            builder.AppendLine($"            var match = {name}.Match(buffer, (byte?)targetSystem, (MavComponent?)((byte?)targetComponent));");
            builder.AppendLine();
            builder.AppendLine("            // Assert");
            builder.AppendLine("            Assert.Equal(expected, match);");
            builder.AppendLine("        }");

            builder.AppendLine("    }");
            builder.Append('}');

            return name;
        }
    }
}