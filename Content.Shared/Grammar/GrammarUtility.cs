namespace Content.Shared.Grammar;

/// <summary>
/// Static utility class containing common methods to apply English grammar in text formatting systems.
/// Not to be confused with <see cref="GrammarSystem"/>.
/// </summary>
public static class GrammarUtility
{
    public static string SanitizeTextCapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // Capitalize first letter
        text = OopsConcat(char.ToUpper(text[0]).ToString(), text.Remove(0, 1));
        return text;
    }

    public static string SanitizeTextEnsureTrailingPeriod(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // Adds a period if the last character is a letter.
        if (char.IsLetter(text[^1]))
            text += ".";
        return text;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }
}
