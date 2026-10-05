using System.Text;

namespace Content.Shared.Utility;

public static class ScrambleUtility
{
    public static string Generate(int length, string chars, int seed)
    {
        if (length <= 0 || chars.Length == 0)
            return string.Empty;

        var random = new System.Random(seed);
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }
}
