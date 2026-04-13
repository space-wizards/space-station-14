using System.Text.RegularExpressions;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Shared helpers used by both Admin Logs and Audit Logs.
/// </summary>
public static class SearchModeHelper
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly char[] WordSeparators = [' '];

    public static bool Matches(string text, string search, LogSearchMode mode)
    {
        if (string.IsNullOrEmpty(search))
            return true;

        return mode switch
        {
            LogSearchMode.Exact => text.Contains(search, StringComparison.OrdinalIgnoreCase),
            LogSearchMode.Regex => TryRegexMatch(text, search),
            LogSearchMode.Wildcard => TryWildcardMatch(text, search),
            _ => MatchesKeyword(text, search),
        };
    }

    /// <summary>
    /// Keyword mode: checks that every whitespace-separated word in search
    /// appears somewhere in the text (case-insensitive).
    /// </summary>
    private static bool MatchesKeyword(string text, string search)
    {
        var words = search.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (!text.Contains(word, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that the text is a legal .NET regex.
    /// </summary>
    public static bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        try
        {
            // Pattern is user-supplied at runtime — cannot be source-generated.
#pragma warning disable RA0026
            _ = new Regex(pattern, RegexOptions.None, RegexTimeout);
#pragma warning restore RA0026
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryRegexMatch(string text, string pattern)
    {
        try
        {
#pragma warning disable RA0026
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase, RegexTimeout);
#pragma warning restore RA0026
        }
        catch
        {
            // Invalid regex.
            return false;
        }
    }

    /// <summary>
    /// Matches using SQL LIKE-style wildcards: % = any characters, _ = one character.
    /// Converts to a regex pattern for evaluation.
    /// </summary>
    private static bool TryWildcardMatch(string text, string pattern)
    {
#pragma warning disable RA0026
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("%", ".*")
            .Replace("_", ".") + "$";

        try
        {
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeout);
#pragma warning restore RA0026
        }
        catch
        {
            return false;
        }
    }
}
