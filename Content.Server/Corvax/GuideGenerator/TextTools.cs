namespace Content.Server.GuideGenerator.TextTools;

public sealed class TextTools
{
    /// <summary>
    /// Capitalizes first letter of given string.
    /// </summary>
    /// <param name="str">String to capitalize</param>
    /// <returns>String with capitalized first letter</returns>
    public static string CapitalizeString(string str)
    {
        if (str.Length > 1)
        {
            return char.ToUpper(str[0]) + str.Remove(0, 1);
        }
        else if (str.Length == 1)
        {
            return char.ToUpper(str[0]).ToString();
        }
        else
        {
            return str;
        }
    }
}
