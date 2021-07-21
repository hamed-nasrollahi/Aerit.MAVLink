using System.Collections.Generic;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class VersionGenerator
    {
        public static void Run(string ns, IEnumerable<VersionDefinition> versions, StringBuilder builder)
        {
            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");
            builder.AppendLine("    public static class Version");
            builder.AppendLine("    {");
            foreach (var version in versions)
            {
                var name = CamelCase(version.FileName.Replace(".xml", ""));
				builder.AppendLine($"        public const byte {name} = {version.Value};");
			}
            builder.AppendLine("    }");
            builder.Append('}');
        }
    }
}