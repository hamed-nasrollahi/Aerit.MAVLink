using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class MessageGenerator
    {
        public static string Run(string ns, MessageDefinition message, StringBuilder builder)
        {
            builder.AppendLine("using System;");

            if (message.Fields.Any(o => o.Type.Name == "char"))
            {
                builder.AppendLine("using System.Text;");
            }

            if (message.Fields.Any(o => o.Type.Name == "double"
                || o.Type.Name == "float"))
            {
                builder.AppendLine("using System.Buffers.Binary;");
            }

            builder.AppendLine();

            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");

            if (message.Description is not null)
            {
                builder.AppendLine("    /// <summary>");
                foreach (var line in message.Description
                    .Split('\n')
                    .Select(o => o.TrimStart()))
                {
                    builder.AppendLine($"    /// {line}");
                }
                builder.AppendLine("    /// </summary>");
            }

            if (message.Deprecated is not null)
            {
                builder.AppendLine($@"    [Obsolete(""{message.Deprecated}"")]");
            }

            var name = CamelCase(message.Name);

            builder.AppendLine($"    public record {name}");
            builder.AppendLine("    {");

            builder.AppendLine("        #region Message Definition");
            builder.AppendLine($@"        public static uint MessageID {{ get; }} = {message.ID};");

            builder.AppendLine();

            builder.AppendLine($@"        public static byte MessageBaseLength {{ get; }} = {message.BaseLength};");

            builder.AppendLine();

            builder.AppendLine($@"        public static byte MessageLength {{ get; }} = {message.Length};");

            builder.AppendLine();

            builder.AppendLine($@"        public static byte MessageCRCExtra {{ get; }} = {message.CRC};");
            builder.AppendLine("        #endregion");

            foreach (var field in message.Fields)
            {
                var fieldName = CamelCase(field.Name);
                if (fieldName == name)
                {
                    fieldName += "_";
                }

                builder.AppendLine();
                if (field.Description is not null)
                {
                    builder.AppendLine("        /// <summary>");
                    foreach (var line in field.Description
                        .Split('\n')
                        .Select(o => o.TrimStart()))
                    {
                        builder.AppendLine($"        /// {field.Description}");
                    }
                    if (field.Units is not null)
                    {
                        builder.AppendLine($"        /// Units: {field.Units}");

                    }
                    builder.AppendLine("        /// </summary>");
                }

                if (field.Enum is not null)
                {
                    if (field.Type.Length is not null)
                    {
                        builder.AppendLine($@"        public {CamelCase(field.Enum)}[] {fieldName} {{ get; init; }}");
                    }
                    else
                    {
                        builder.AppendLine($@"        public {CamelCase(field.Enum)} {fieldName} {{ get; init; }}");
                    }
                }
                else if (field.Type.Name == "char")
                {
                    builder.AppendLine($@"        public string {fieldName} {{ get; init; }}");
                }
                else
                {
                    if (field.Type.Length is not null)
                    {
                        builder.AppendLine($@"        public {field.Type.CS}[] {fieldName} {{ get; init; }}");
                    }
                    else
                    {
                        builder.AppendLine($@"        public {field.Type.CS} {fieldName} {{ get; init; }}");
                    }
                }
            }

            builder.AppendLine();

            builder.AppendLine("        public Memory<byte> Serialize(Memory<byte> buffer)");
            builder.AppendLine("        {");
            builder.AppendLine("            var span = buffer.Span;");
            builder.AppendLine();

            var index = 0;

            foreach (var field in message.Fields)
            {
                var fieldName = CamelCase(field.Name);
                if (fieldName == name)
                {
                    fieldName += "_";
                }

                switch (field.Type.Name)
                {
                    case "uint64_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 24);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 32);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 40);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 48);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName}[{i}] >> 56);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 24);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 32);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 40);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 48);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 56);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 24);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 32);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 40);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 48);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ulong){fieldName} >> 56);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 24);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 32);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 40);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 48);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 56);");
                            }
                        }
                        break;

                    case "int64_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 24);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 32);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 40);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 48);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName}[{i}] >> 56);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 24);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 32);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 40);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 48);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 56);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 24);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 32);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 40);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 48);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((long){fieldName} >> 56);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 24);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 32);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 40);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 48);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 56);");
                            }
                        }
                        break;

                    case "double":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                builder.AppendLine($@"            BinaryPrimitives.WriteDoubleBigEndian(span.Slice({index}, 8), {fieldName}[{i}]);");
                                index += 8;
                            }
                        }
                        else
                        {
                            builder.AppendLine($@"            BinaryPrimitives.WriteDoubleBigEndian(span.Slice({index}, 8), {fieldName});");
                            index += 8;
                        }
                        break;

                    case "uint32_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName}[{i}] >> 24);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 24);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((uint){fieldName} >> 24);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 24);");
                            }
                        }
                        break;

                    case "int32_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName}[{i}] >> 24);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 16);");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 24);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)((int){fieldName} >> 24);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 16);");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 24);");
                            }
                        }
                        break;

                    case "float":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                builder.AppendLine($@"            BinaryPrimitives.WriteSingleBigEndian(span.Slice({index}, 4), {fieldName}[{i}]);");
                                index += 4;
                            }
                        }
                        else
                        {
                            builder.AppendLine($@"            BinaryPrimitives.WriteSingleBigEndian(span.Slice({index}, 4), {fieldName});");
                            index += 4;
                        }
                        break;

                    case "uint16_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((ushort){fieldName}[{i}] >> 8);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((ushort){fieldName} >> 8);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                            }
                        }
                        break;

                    case "int16_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)((short){fieldName}[{i}] >> 8);");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                    builder.AppendLine($@"            span[{index++}] = (byte)({fieldName}[{i}] >> 8);");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)((short){fieldName} >> 8);");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                                builder.AppendLine($@"            span[{index++}] = (byte)({fieldName} >> 8);");
                            }
                        }
                        break;

                    case "uint8_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = {fieldName}[{i}];");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = {fieldName};");
                            }
                        }
                        break;

                    case "int8_t":
                        if (field.Type.Length is not null)
                        {
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (field.Enum is not null)
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                }
                                else
                                {
                                    builder.AppendLine($@"            span[{index++}] = (byte){fieldName}[{i}];");
                                }
                            }
                        }
                        else
                        {
                            if (field.Enum is not null)
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                            }
                            else
                            {
                                builder.AppendLine($@"            span[{index++}] = (byte){fieldName};");
                            }
                        }
                        break;

                    case "char":
                        builder.AppendLine($@"            if ({fieldName}.Length < {field.Type.Length})");
                        builder.AppendLine("            {");
                        builder.AppendLine($@"                Encoding.ASCII.GetBytes({fieldName}, span.Slice({index}, {fieldName}.Length));");
                        builder.AppendLine($@"                span[({index} + {fieldName}.Length)..({index} + {field.Type.Length})].Fill(0x00);");
                        builder.AppendLine("            }");
                        builder.AppendLine("            else");
                        builder.AppendLine("            {");
                        builder.AppendLine($@"                Encoding.ASCII.GetBytes({fieldName}.AsSpan(0, {field.Type.Length}), span.Slice({index}, {field.Type.Length}));");
                        builder.AppendLine("            }");

                        index += field.Type.Length ?? 0;

                        break;

                    case "uint8_t_mavlink_version":
                        builder.AppendLine($@"            span[{index++}] = {fieldName};");
                        break;

                    default:
                        break;
                }
            }

            builder.AppendLine();
            builder.AppendLine("            var payload = buffer.TrimEnd((byte)0x00);");
            builder.AppendLine("            if (payload.Length > 0)");
            builder.AppendLine("            {");
            builder.AppendLine("                return payload;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            span[0] = 0x00;");
            builder.AppendLine();
            builder.AppendLine("            return buffer.Slice(0, 1);");
            builder.AppendLine("        }");

            builder.AppendLine();

            builder.AppendLine($"        public static {name} Deserialize(ReadOnlySpan<byte> span)");
            builder.AppendLine("        {");

            index = 0;

            var assignments = new List<string>();

            foreach (var field in message.Fields)
            {
                var fieldName = CamelCase(field.Name);
                if (fieldName == name)
                {
                    fieldName += "_";
                }

                var fieldLowerName = fieldName.Length > 1
                    ? char.ToLower(fieldName[0]) + fieldName[1..]
                    : fieldName.ToLower();

                if (fieldLowerName == "fixed")
                {
                    fieldLowerName = "@fixed";
                }

                switch (field.Type.Name)
                {
                    case "uint64_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? (ulong)span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (ulong)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (ulong)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (ulong)0x00) << 24");
                                    builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (ulong)0x00) << 32");
                                    builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (ulong)0x00) << 40");
                                    builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (ulong)0x00) << 48");
                                    builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (ulong)0x00) << 56);");

                                    index += 8;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? (ulong)span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (ulong)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (ulong)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (ulong)0x00) << 24");
                                builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (ulong)0x00) << 32");
                                builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (ulong)0x00) << 40");
                                builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (ulong)0x00) << 48");
                                builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (ulong)0x00) << 56);");

                                index += 8;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new ulong[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (span.Length >= {index + 1} ? (ulong)span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (ulong)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (ulong)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (ulong)0x00) << 24");
                                    builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (ulong)0x00) << 32");
                                    builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (ulong)0x00) << 40");
                                    builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (ulong)0x00) << 48");
                                    builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (ulong)0x00) << 56;");

                                    index += 8;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (span.Length >= {index + 1} ? (ulong)span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (ulong)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (ulong)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (ulong)0x00) << 24");
                                builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (ulong)0x00) << 32");
                                builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (ulong)0x00) << 40");
                                builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (ulong)0x00) << 48");
                                builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (ulong)0x00) << 56;");

                                index += 8;
                            }
                        }
                        break;

                    case "int64_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (long)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (long)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (long)0x00) << 24");
                                    builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (long)0x00) << 32");
                                    builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (long)0x00) << 40");
                                    builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (long)0x00) << 48");
                                    builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (long)0x00) << 56);");

                                    index += 8;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (long)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (long)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (long)0x00) << 24");
                                builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (long)0x00) << 32");
                                builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (long)0x00) << 40");
                                builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (long)0x00) << 48");
                                builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (long)0x00) << 56);");

                                index += 8;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new ulong[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (long)(span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (long)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (long)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (long)0x00) << 24");
                                    builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (long)0x00) << 32");
                                    builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (long)0x00) << 40");
                                    builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (long)0x00) << 48");
                                    builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (long)0x00) << 56;");

                                    index += 8;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (long)(span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (long)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (long)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (long)0x00) << 24");
                                builder.AppendLine($"                | (span.Length >= {index + 5} ? span[{index + 4}] : (long)0x00) << 32");
                                builder.AppendLine($"                | (span.Length >= {index + 6} ? span[{index + 5}] : (long)0x00) << 40");
                                builder.AppendLine($"                | (span.Length >= {index + 7} ? span[{index + 6}] : (long)0x00) << 48");
                                builder.AppendLine($"                | (span.Length >= {index + 8} ? span[{index + 7}] : (long)0x00) << 56;");

                                index += 8;
                            }
                        }
                        break;

                    case "double":
                        if (field.Type.Length is not null)
                        {
                            builder.AppendLine($"            var {fieldLowerName} = new double[{field.Type.Length}];");
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (index > 0)
                                {
                                    builder.AppendLine($"            if ((span.Length - {index}) < 8)");
                                }
                                else
                                {
                                    builder.AppendLine($"            if (span.Length < 8)");
                                }
                                builder.AppendLine("            {");
                                builder.AppendLine("                Span<byte> bytes = stackalloc byte[8];");
                                builder.AppendLine($"                span[{index}..].CopyTo(bytes);");
                                builder.AppendLine($"                {fieldLowerName}[{i}] = BinaryPrimitives.ReadDoubleBigEndian(bytes);");
                                builder.AppendLine("            }");
                                builder.AppendLine("            else");
                                builder.AppendLine("            {");
                                builder.AppendLine($"                {fieldLowerName}[{i}] = BinaryPrimitives.ReadDoubleBigEndian(span.Slice({index}, 8));");
                                builder.AppendLine("            }");

                                index += 8;
                            }
                        }
                        else
                        {
                            builder.AppendLine($"            double {fieldLowerName};");

                            if (index > 0)
                            {
                                builder.AppendLine($"            if ((span.Length - {index}) < 8)");
                            }
                            else
                            {
                                builder.AppendLine($"            if (span.Length < 8)");
                            }
                            builder.AppendLine("            {");
                            builder.AppendLine("                Span<byte> bytes = stackalloc byte[8];");
                            builder.AppendLine($"                span[{index}..].CopyTo(bytes);");
                            builder.AppendLine($"                {fieldLowerName} = BinaryPrimitives.ReadDoubleBigEndian(bytes);");
                            builder.AppendLine("            }");
                            builder.AppendLine("            else");
                            builder.AppendLine("            {");
                            builder.AppendLine($"                {fieldLowerName} = BinaryPrimitives.ReadDoubleBigEndian(span.Slice({index}, 8));");
                            builder.AppendLine("            }");

                            index += 8;
                        }
                        break;

                    case "uint32_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? (uint)span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (uint)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (uint)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (uint)0x00) << 24);");

                                    index += 4;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? (uint)span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (uint)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (uint)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (uint)0x00) << 24);");

                                index += 4;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new uint[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (span.Length >= {index + 1} ? (uint)span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (uint)0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (uint)0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (uint)0x00) << 24;");

                                    index += 4;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (span.Length >= {index + 1} ? (uint)span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : (uint)0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : (uint)0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : (uint)0x00) << 24;");

                                index += 4;
                            }
                        }
                        break;

                    case "int32_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : 0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : 0x00) << 24);");

                                    index += 4;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : 0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : 0x00) << 24);");

                                index += 4;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new int[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8");
                                    builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : 0x00) << 16");
                                    builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : 0x00) << 24;");

                                    index += 4;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8");
                                builder.AppendLine($"                | (span.Length >= {index + 3} ? span[{index + 2}] : 0x00) << 16");
                                builder.AppendLine($"                | (span.Length >= {index + 4} ? span[{index + 3}] : 0x00) << 24;");

                                index += 4;
                            }
                        }
                        break;

                    case "float":
                        if (field.Type.Length is not null)
                        {
                            builder.AppendLine($"            var {fieldLowerName} = new float[{field.Type.Length}];");
                            for (var i = 0; i < field.Type.Length; i++)
                            {
                                if (index > 0)
                                {
                                    builder.AppendLine($"            if ((span.Length - {index}) < 4)");
                                }
                                else
                                {
                                    builder.AppendLine($"            if (span.Length < 4)");
                                }
                                builder.AppendLine("            {");
                                builder.AppendLine("                Span<byte> bytes = stackalloc byte[4];");
                                builder.AppendLine($"                span[{index}..].CopyTo(bytes);");
                                builder.AppendLine($"                {fieldLowerName}[{i}] = BinaryPrimitives.ReadSingleBigEndian(bytes);");
                                builder.AppendLine("            }");
                                builder.AppendLine("            else");
                                builder.AppendLine("            {");
                                builder.AppendLine($"                {fieldLowerName}[{i}] = BinaryPrimitives.ReadSingleBigEndian(span.Slice({index}, 4));");
                                builder.AppendLine("            }");

                                index += 4;
                            }
                        }
                        else
                        {
                            builder.AppendLine($"            float {fieldLowerName};");

                            if (index > 0)
                            {
                                builder.AppendLine($"            if ((span.Length - {index}) < 4)");
                            }
                            else
                            {
                                builder.AppendLine($"            if (span.Length < 4)");
                            }
                            builder.AppendLine("            {");
                            builder.AppendLine("                Span<byte> bytes = stackalloc byte[4];");
                            builder.AppendLine($"                span[{index}..].CopyTo(bytes);");
                            builder.AppendLine($"                {fieldLowerName} = BinaryPrimitives.ReadSingleBigEndian(bytes);");
                            builder.AppendLine("            }");
                            builder.AppendLine("            else");
                            builder.AppendLine("            {");
                            builder.AppendLine($"                {fieldLowerName} = BinaryPrimitives.ReadSingleBigEndian(span.Slice({index}, 4));");
                            builder.AppendLine("            }");

                            index += 4;
                        }
                        break;

                    case "uint16_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                    index += 2;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                index += 2;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new ushort[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (ushort)((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                    index += 2;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (ushort)((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                index += 2;
                            }
                        }
                        break;

                    case "int16_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                    index += 2;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                index += 2;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new short[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (short)((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                    builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                    index += 2;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (short)((span.Length >= {index + 1} ? span[{index}] : 0x00)");
                                builder.AppendLine($"                | (span.Length >= {index + 2} ? span[{index + 1}] : 0x00) << 8);");

                                index += 2;
                            }
                        }
                        break;

                    case "uint8_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                    index++;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                index++;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new byte[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (byte)(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                    index++;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (byte)(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                index++;
                            }
                        }
                        break;

                    case "int8_t":
                        if (field.Enum is not null)
                        {
                            var enumName = CamelCase(field.Enum);

                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new {enumName}[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = ({enumName})(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                    index++;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = ({enumName})(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                index++;
                            }
                        }
                        else
                        {
                            if (field.Type.Length is not null)
                            {
                                builder.AppendLine($"            var {fieldLowerName} = new sbyte[{field.Type.Length}];");
                                for (var i = 0; i < field.Type.Length; i++)
                                {
                                    builder.AppendLine($"            {fieldLowerName}[{i}] = (sbyte)(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                    index++;
                                }
                            }
                            else
                            {
                                builder.AppendLine($"            var {fieldLowerName} = (sbyte)(span.Length >= {index + 1} ? span[{index}] : 0x00);");

                                index++;
                            }
                        }
                        break;

                    case "char":
                        builder.AppendLine($"            string {fieldLowerName};");
                        if (index > 0)
                        {
                            builder.AppendLine($"            if ((span.Length - {index}) < {field.Type.Length})");
                        }
                        else
                        {
                            builder.AppendLine($"            if (span.Length < {field.Type.Length})");
                        }
                        builder.AppendLine("            {");
                        builder.AppendLine($"                {fieldLowerName} = Encoding.ASCII.GetString(span[{index}..]);");
                        builder.AppendLine("            }");
                        builder.AppendLine("            else");
                        builder.AppendLine("            {");
                        builder.AppendLine($"                var bytes = span.Slice({index}, {field.Type.Length});");
                        builder.AppendLine("                var end = bytes.IndexOf((byte)0x00);");
                        builder.AppendLine("                if (end > 0)");
                        builder.AppendLine("                {");
                        builder.AppendLine("                    bytes = bytes.Slice(0, end);");
                        builder.AppendLine("                }");
                        builder.AppendLine($"                {fieldLowerName} = Encoding.ASCII.GetString(bytes);");
                        builder.AppendLine("            }");

                        index += field.Type.Length ?? 0;
                        break;

                    case "uint8_t_mavlink_version":
                        builder.AppendLine($"            var {fieldLowerName} = (byte)(span.Length >= {index + 1} ? span[{index}] : 0x00);");
                        break;

                    default:
                        break;
                }

                builder.AppendLine();

                assignments.Add($"              {fieldName} = {fieldLowerName}");
            }

            builder.AppendLine("            return new()");
            builder.AppendLine("            {");
            builder.AppendLine(string.Join(",\n", assignments));
            builder.AppendLine("            };");
            builder.AppendLine("        }");

            builder.AppendLine("    }");
            builder.Append('}');

            return name;
        }
    }
}