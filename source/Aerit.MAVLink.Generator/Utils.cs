using System.Linq;

namespace Aerit.MAVLink.Generator
{
    public static class Utils
    {
        public static string CamelCase(string value, string? prefix = null)
        {
            if (prefix is not null && value.StartsWith(prefix))
            {
                value = value[prefix.Length..];
            }

            var words = value
                .Split('_')
                .Select(word =>
                    word.Length > 1
                        ? char.ToUpper(word[0]) + word[1..].ToLower()
                        : word.ToUpper());

            return string.Join(string.Empty, words);
        }
    }
}