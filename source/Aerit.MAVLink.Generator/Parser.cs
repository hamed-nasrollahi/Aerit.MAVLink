using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Aerit.MAVLink.Utils;

namespace Aerit.MAVLink.Generator
{
    public static class Parser
    {
        private static FieldDefinition ParseFieldDefinition(XElement field, byte index, bool extension)
        {
            var name = (string?)field.Attribute("name") ?? throw new Exception("Invalid field Name");

            var type = (string?)field.Attribute("type") ?? throw new Exception("Invalid field Type");
            byte? length = null;

            var arrayStart = type.IndexOf('[');
            if (arrayStart > 0)
            {
                length = byte.Parse(type[(arrayStart + 1)..^1]);
                type = type[..arrayStart];
            }

            var description = field.Value;

            var _enum = (string?)field.Attribute("enum");
            var units = (string?)field.Attribute("units");
            var _default = (string?)field.Attribute("default");

            return new(
                index,
                new(type, length),
                name,
                description,
                _enum,
                units,
                _default,
                extension
            );
        }

        private static DeprecatedDefinition? ParseDeprecatedDefinition(XElement? deprecated)
        {
            if (deprecated is null)
            {
                return null;
            }

            var description = (string?)deprecated.Value;
            var since = (string?)deprecated.Attribute("since") ?? throw new Exception("Invalid deprecated Since");
            var replaced_by = (string?)deprecated.Attribute("replaced_by") ?? throw new Exception("Invalid deprecated Replaced_By");

            return new(description, since, replaced_by);
        }

        private static MessageDefinition ParseMessageDefinition(XElement message)
        {
            var id = (uint?)message.Attribute("id") ?? throw new Exception("Invalid message ID");
            var name = (string?)message.Attribute("name") ?? throw new Exception("Invalid message Name");

            DeprecatedDefinition? deprecated = null;

            string? description = null;

            var fields = new List<FieldDefinition>();

            List<FieldDefinition>? extensionFields = null;

            byte index = 0;
            var extension = false;

            byte payloadBaseLength = 0;
            byte payloadLength = 0;

            foreach (var element in message.Elements())
            {
                if (element.Name == "deprecated")
                {
                    deprecated = ParseDeprecatedDefinition(element);
                }
                else if (element.Name == "description")
                {
                    description = element.Value;
                }
                else if (element.Name == "field")
                {
                    var field = ParseFieldDefinition(element, index++, extension);

                    var size = (byte)(field.Type.Size * (field.Type.Length ?? 1));

                    if (extension)
                    {
                        extensionFields?.Add(field);
                    }
                    else
                    {
                        fields.Add(field);

                        payloadBaseLength += size;
                    }

                    payloadLength += size;

                    index++;
                }
                else if (element.Name == "extensions")
                {
                    extension = true;
                    extensionFields = new List<FieldDefinition>();
                }
            }

            var ordered = fields
                .OrderByDescending(o => o.Type.Size)
                .ThenBy(o => o.Index)
                .ToList();

            var checksum = Checksum.Seed;

            foreach (var c in name)
            {
                checksum = Checksum.Compute((byte)c, checksum);
            }

            checksum = Checksum.Compute((byte)' ', checksum);

            foreach (var field in ordered)
            {
                foreach (var c in field.Type.Name)
                {
                    checksum = Checksum.Compute((byte)c, checksum);
                }

                checksum = Checksum.Compute((byte)' ', checksum);

                foreach (var c in field.Name)
                {
                    checksum = Checksum.Compute((byte)c, checksum);
                }

                checksum = Checksum.Compute((byte)' ', checksum);

                if (field.Type.Length is byte length)
                {
                    checksum = Checksum.Compute(length, checksum);
                }
            }

            var crc = (byte)((checksum & 0xff) ^ (checksum >> 8));

            if (extensionFields is not null)
            {
                ordered.AddRange(extensionFields);
            }

            return new(id, name, description, ordered, payloadBaseLength, payloadLength, crc, deprecated);
        }

        private static ParamDefinition? ParseParamDefinition(XElement param)
            => new(
                param.Value,
                (string?)param.Attribute("label"),
                (string?)param.Attribute("units"),
                (string?)param.Attribute("enum"),
                (string?)param.Attribute("increment"),
                (string?)param.Attribute("minValue"),
                (string?)param.Attribute("maxValue"),
                (string?)param.Attribute("default"));

        private static EntryDefinition ParseEntryDefinition(XElement entry)
        {
            var name = (string?)entry.Attribute("name") ?? throw new Exception("Invalid entry Name");
            var description = entry.Element("description")?.Value;
            var value = (string?)entry.Attribute("value");
            var deprecated = ParseDeprecatedDefinition(entry.Element("deprecated"));

            ParamDefinition?[]? _params = null;

            var elements = entry.Elements("param");
            if (elements.Any())
            {
                _params = new ParamDefinition?[7];
                foreach (var param in elements)
                {
                    var index = (int?)param.Attribute("index") ?? throw new Exception("Missing parameter Index");

                    _params[index - 1] = ParseParamDefinition(param);
                }
            }

            return new(name, description, value, _params, deprecated);
        }

        private static EnumDefinition ParseEnumDefinition(XElement _enum, IDictionary<string, FieldType> baseTypes)
        {
            var name = (string?)_enum.Attribute("name") ?? throw new Exception("Invalid enum Name");

            baseTypes.TryGetValue(name, out FieldType? type);

            var bitmask = (string?)_enum.Attribute("bitmask") == "true";

            var description = _enum.Element("description")?.Value;

            var entries = new List<EntryDefinition>();

            foreach (var entry in _enum.Elements("entry"))
            {
                entries.Add(ParseEntryDefinition(entry));
            }

            var deprecated = ParseDeprecatedDefinition(_enum.Element("deprecated"));

            return new(name, type, bitmask, description, entries, deprecated);
        }

        public static (List<MessageDefinition> messages, List<EnumDefinition> enums) Run(string path, string fn, Dictionary<string, FieldType>? enumBaseTypes = null)
        {
            if (enumBaseTypes is null)
            {
                enumBaseTypes = new Dictionary<string, FieldType>();
            }

            var doc = XDocument.Load(Path.Combine(path, fn));

            var messages = new List<MessageDefinition>();
            var enums = new List<EnumDefinition>();

            var include = doc?.Root?.Element("include");
            if (include is not null)
            {
                var (includedMessages, includedEnums) = Run(path, include!.Value, enumBaseTypes);

                messages.AddRange(includedMessages);
                enums.AddRange(includedEnums);
            }

            foreach (var element in doc?.Root?.Element("messages")?.Elements("message")!)
            {
                var message = ParseMessageDefinition(element);

                var mapping = message
                    .Fields
                    ?.Where(o => o.Enum is not null)
                    .Select(o => (_enum: o.Enum!, type: o.Type.Name))
                    .Distinct()
                    .ToList();

                foreach (var (_enum, type) in mapping!)
                {
                    enumBaseTypes[_enum] = new(type);
                }

                messages.Add(message);
            }

            foreach (var element in doc?.Root?.Element("enums")?.Elements("enum")!)
            {
                enums.Add(ParseEnumDefinition(element, enumBaseTypes));
            }

            return (messages, enums);
        }
    }
}