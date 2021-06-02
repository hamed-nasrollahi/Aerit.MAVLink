using System.IO;
using System.Text;

namespace Aerit.MAVLink.Generator
{
    public static class Generator
    {
        public record Destination(
            string Enums,
            string Messages,
            string? Tests = null);

        public record Config(
            (string path, string file) Definitions,
            Destination Destination,
            string NameSpace = "Aerit.MAVLink",
            bool TestDeprecated = false);

        public static void Run(Config config)
        {
            var (messages, enums) = Parser.Run(config.Definitions.path, config.Definitions.file);

            var builder = new StringBuilder();

            foreach (var _enum in enums)
            {
                var name = EnumGenerator.Run(config.NameSpace, _enum, builder);

                File.WriteAllText(Path.Combine(config.Destination.Enums, $"{name}.cs"), builder.ToString());

                builder.Clear();
            }

            foreach (var message in messages)
            {
                var name = MessageGenerator.Run(config.NameSpace, message, builder);

                File.WriteAllText(Path.Combine(config.Destination.Messages, $"{name}.cs"), builder.ToString());

                builder.Clear();

                if (config.Destination.Tests is not null)
                {
                    if (!config.TestDeprecated && message.Deprecated is not null)
                    {
                        continue;
                    }
                    
                    name = MessageTestGenerator.Run(config.NameSpace, message, builder);

                    File.WriteAllText(Path.Combine(config.Destination.Tests, $"{name}Test.cs"), builder.ToString());

                    builder.Clear();
                }
            }
        }
    }
}