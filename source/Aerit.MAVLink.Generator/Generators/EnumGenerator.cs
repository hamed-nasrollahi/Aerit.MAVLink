using System.Linq;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class EnumGenerator
    {
        public static string Run(string ns, EnumDefinition _enum, StringBuilder builder)
        {
            if (_enum.Bitmask
                || _enum.Deprecated is not null
                || _enum.Entries.Any(o => o.Deprecated is not null))
            {
                builder.AppendLine("using System;");
                builder.AppendLine();
            }

            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");

            if (_enum.Description is not null)
            {
                builder.AppendLine("    /// <summary>");
                foreach (var line in _enum.Description
                        .Split('\n')
                        .Select(o => o.TrimStart()))
                {
                    builder.AppendLine($"    /// {line}");
                }
                builder.AppendLine("    /// </summary>");
            }

            if (_enum.Deprecated is not null)
            {
                builder.AppendLine($@"    [Obsolete(""{_enum.Deprecated}"")]");
            }

            if (_enum.Bitmask)
            {
                builder.AppendLine("    [Flags]");
            }

            var name = CamelCase(_enum.Name);

            builder.Append($"    public enum {name}");
            if (_enum.Type is not null)
            {
                builder.Append($" : {_enum.Type.CS}");
            }
            builder.AppendLine();
            builder.AppendLine("    {");

            var first = true;

            foreach (var entry in _enum.Entries)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.AppendLine(",");
                }

                if (entry.Description is not null)
                {
                    builder.AppendLine("        /// <summary>");
                    foreach (var line in entry.Description
                        .Split('\n')
                        .Select(o => o.TrimStart()))
                    {
                        builder.AppendLine($"       /// {line}");
                    }
                    builder.AppendLine("        /// </summary>");
                }

                if (entry.Deprecated is not null)
                {
                    builder.AppendLine($@"        [Obsolete(""{entry.Deprecated}"")]");
                }

                var entryName = CamelCase(entry.Name, _enum.Name);
                if (char.IsDigit(entryName[0]))
                {
                    entryName = "_" + entryName;
                }

                builder.Append("        ");
                builder.Append(entryName);

                if (entry.Value is not null)
                {
                    builder.Append(" = ");
                    builder.Append(entry.Value);
                }
            }

            builder.AppendLine();
            builder.AppendLine("    }");
            builder.Append('}');

            return name;
        }
    }
}