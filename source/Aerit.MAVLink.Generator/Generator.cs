using System.IO;
using System.Linq;
using System.Text;

namespace Aerit.MAVLink.Generator
{
	public static class Generator
	{
		public record Destination(
			string Generated,
			string Enums,
			string Messages,
			string Commands,
			string? Tests = null);

		public record Config(
			(string path, string file) Definitions,
			Destination Destination,
			string Namespace,
			bool TestDeprecated);

		public static void Run(Config config)
		{
			var (messages, enums) = Parser.Run(config.Definitions.path, config.Definitions.file);

			var builder = new StringBuilder();

			foreach (var _enum in enums)
			{
				var name = EnumGenerator.Run(config.Namespace, _enum, builder);

				File.WriteAllText(Path.Combine(config.Destination.Enums, $"{name}.cs"), builder.ToString());

				builder.Clear();
			}

			foreach (var message in messages)
			{
				var name = MessageGenerator.Run(config.Namespace, message, builder);

				File.WriteAllText(Path.Combine(config.Destination.Messages, $"{name}.cs"), builder.ToString());

				builder.Clear();

				if (config.Destination.Tests is not null)
				{
					if (!config.TestDeprecated && message.Deprecated is not null)
					{
						continue;
					}

					name = MessageTestsGenerator.Run(config.Namespace, message, builder);

					File.WriteAllText(Path.Combine(config.Destination.Tests, $"{name}Tests.cs"), builder.ToString());

					builder.Clear();
				}
			}

			CRCExtraGenerator.Run(config.Namespace, messages, builder);
			File.WriteAllText(Path.Combine(config.Destination.Generated, "CRCExtra.cs"), builder.ToString());
			builder.Clear();

			TargetMatchGenerator.Run(config.Namespace, messages, builder);
			File.WriteAllText(Path.Combine(config.Destination.Generated, "TargetMatch.cs"), builder.ToString());
			builder.Clear();

			var mavCmd = enums.FirstOrDefault(o => o.Name == "MAV_CMD");
			if (mavCmd is not null)
			{
				foreach (var cmd in mavCmd.Entries)
				{
					if (!int.TryParse(cmd.Value, out var value) || value < 60100)
					{
						continue;
					}

					var name = CommandGenerator.Run(config.Namespace, cmd, builder);
					if (!string.IsNullOrEmpty(name))
					{
						File.WriteAllText(Path.Combine(config.Destination.Commands, $"{name}.cs"), builder.ToString());
					}

					builder.Clear();
				}
			}
		}
	}
}