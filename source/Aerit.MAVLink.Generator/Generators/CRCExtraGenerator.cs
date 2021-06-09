using System.Collections.Generic;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    using static Utils;

    public static class CRCExtraGenerator
    {
        public static void Run(string ns, IEnumerable<MessageDefinition> messages, StringBuilder builder)
        {
            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");
            builder.AppendLine("    public static class CRCExtra");
            builder.AppendLine("    {");
            builder.AppendLine("        public static byte? GetByMessageId(uint messageId) => messageId switch");
            builder.AppendLine("        {");
            foreach (var message in messages)
            {
                var name = CamelCase(message.Name);
                builder.AppendLine($"            {name}.MAVLinkMessageId => {name}.MAVLinkMessageCRCExtra,");
            }
            builder.AppendLine("            _ => null");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.Append('}');
        }
    }
}