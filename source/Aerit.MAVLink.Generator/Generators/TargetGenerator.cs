using System.Collections.Generic;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class TargetMatchGenerator
    {
        public static void Run(string ns, IEnumerable<MessageDefinition> messages, StringBuilder builder)
        {
            builder.AppendLine("using System;");
			builder.AppendLine();
			builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");
            builder.AppendLine("    public static class Target");
            builder.AppendLine("    {");
            builder.AppendLine("        public static (byte? targetSystem, byte? targetComponent) Deserialize(uint messageId, ReadOnlySpan<byte> payload) => messageId switch");
            builder.AppendLine("        {");
            foreach (var message in messages)
            {
                var name = CamelCase(message.Name);
                builder.AppendLine($"            {name}.MAVLinkMessageId => {name}.DeserializeTarget(payload),");
            }
            builder.AppendLine("            _ => default");
            builder.AppendLine("        };");
			builder.AppendLine();
            builder.AppendLine("        public static bool Match(uint messageId, ReadOnlySpan<byte> payload, byte? targetSystem, MavComponent? targetComponent) => messageId switch");
            builder.AppendLine("        {");
            foreach (var message in messages)
            {
                var name = CamelCase(message.Name);
                builder.AppendLine($"            {name}.MAVLinkMessageId => {name}.Match(payload, targetSystem, targetComponent),");
            }
            builder.AppendLine("            _ => false");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.Append('}');
        }
    }
}