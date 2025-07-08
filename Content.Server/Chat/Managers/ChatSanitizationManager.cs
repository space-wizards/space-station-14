using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.Managers;

/// <summary>
///     Sanitizes messages!
///     It currently ony removes the shorthands for emotes (like "lol" or "^-^") from a chat message and returns the last
///     emote in their message
/// </summary>
public sealed class ChatSanitizationManager : IChatSanitizationManager
{
    private static readonly Dictionary<string, string> ShorthandToEmote = new()
    {
        { ":)", "chatsan-smiles" },
        { ":]", "chatsan-smiles" },
        { "=)", "chatsan-smiles" },
        { "=]", "chatsan-smiles" },
        { "(:", "chatsan-smiles" },
        { "[:", "chatsan-smiles" },
        { "(=", "chatsan-smiles" },
        { "[=", "chatsan-smiles" },
        { "^^", "chatsan-smiles" },
        { "^-^", "chatsan-smiles" },
        { ":(", "chatsan-frowns" },
        { ":[", "chatsan-frowns" },
        { "=(", "chatsan-frowns" },
        { "=[", "chatsan-frowns" },
        { "):", "chatsan-frowns" },
        { ")=", "chatsan-frowns" },
        { "]:", "chatsan-frowns" },
        { "]=", "chatsan-frowns" },
        { ":D", "chatsan-smiles-widely" },
        { "D:", "chatsan-frowns-deeply" },
        { ":O", "chatsan-surprised" },
        { ":3", "chatsan-smiles" },
        { ":S", "chatsan-uncertain" },
        { ":>", "chatsan-grins" },
        { ":<", "chatsan-pouts" },
        { "xD", "chatsan-laughs" },
        { ":'(", "chatsan-cries" },
        { ":'[", "chatsan-cries" },
        { "='(", "chatsan-cries" },
        { "='[", "chatsan-cries" },
        { ")':", "chatsan-cries" },
        { "]':", "chatsan-cries" },
        { ")'=", "chatsan-cries" },
        { "]'=", "chatsan-cries" },
        { ";-;", "chatsan-cries" },
        { ";_;", "chatsan-cries" },
        { "qwq", "chatsan-cries" },
        { ":u", "chatsan-smiles-smugly" },
        { ":v", "chatsan-smiles-smugly" },
        { ">:i", "chatsan-annoyed" },
        { ":i", "chatsan-sighs" },
        { ":|", "chatsan-sighs" },
        { ":p", "chatsan-stick-out-tongue" },
        { ";p", "chatsan-stick-out-tongue" },
        { ":b", "chatsan-stick-out-tongue" },
        { "0-0", "chatsan-wide-eyed" },
        { "o-o", "chatsan-wide-eyed" },
        { "o.o", "chatsan-wide-eyed" },
        { "._.", "chatsan-surprised" },
        { ".-.", "chatsan-confused" },
        { "-_-", "chatsan-unimpressed" },
        { "smh", "chatsan-unimpressed" },
        { "o/", "chatsan-waves" },
        { "^^/", "chatsan-waves" },
        { ":/", "chatsan-uncertain" },
        { ":\\", "chatsan-uncertain" },
        { "lmao", "chatsan-laughs" },
        { "lmfao", "chatsan-laughs" },
        { "lol", "chatsan-laughs" },
        { "lel", "chatsan-laughs" },
        { "kek", "chatsan-laughs" },
        { "rofl", "chatsan-laughs" },
        { "o7", "chatsan-salutes" },
        { ";_;7", "chatsan-tearfully-salutes" },
        { "idk", "chatsan-shrugs" },
        { ";)", "chatsan-winks" },
        { ";]", "chatsan-winks" },
        { "(;", "chatsan-winks" },
        { "[;", "chatsan-winks" },
        { ":')", "chatsan-tearfully-smiles" },
        { ":']", "chatsan-tearfully-smiles" },
        { "=')", "chatsan-tearfully-smiles" },
        { "=']", "chatsan-tearfully-smiles" },
        { "(':", "chatsan-tearfully-smiles" },
        { "[':", "chatsan-tearfully-smiles" },
        { "('=", "chatsan-tearfully-smiles" },
        { "['=", "chatsan-tearfully-smiles" }
    };

    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private bool _doSanitize;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ChatSanitizerEnabled, x => _doSanitize = x, true);
    }

    /// <summary>
    ///     Remove the shorthands from the message, returning the last one found as the emote
    /// </summary>
    /// <param name="message">The pre-sanitized message</param>
    /// <param name="speaker">The speaker</param>
    /// <param name="sanitized">The sanitized message with shorthands removed</param>
    /// <param name="emote">The localized emote</param>
    /// <returns>True if emote has been sanitized out</returns>
    public bool TrySanitizeEmoteShorthands(string message,
        EntityUid speaker,
        out string sanitized,
        [NotNullWhen(true)] out string? emote)
    {
        emote = null;
        sanitized = message;

        if (!_doSanitize)
            return false;

        // -1 is just a canary for nothing found yet
        var lastEmoteIndex = -1;

        foreach (var (shorthand, emoteKey) in ShorthandToEmote)
        {
            // We have to escape it because shorthands like ":)" or "-_-" would break the regex otherwise.
            var escaped = Regex.Escape(shorthand);

            // So there are 2 cases:
            // - If there is whitespace before it and after it is either punctuation, whitespace, or the end of the line
            //   Delete the word and the whitespace before
            // - If it is at the start of the string and is followed by punctuation, whitespace, or the end of the line
            //   Delete the word and the punctuation if it exists.
            var pattern =
                $@"\s{escaped}(?=\p{{P}}|\s|$)|^{escaped}(?:\p{{P}}|(?=\s|$))";

            var r = new Regex(pattern, RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

            // We're using sanitized as the original message until the end so that we can make sure the indices of
            // the emotes are accurate.
            var lastMatch = r.Match(sanitized);

            if (!lastMatch.Success)
                continue;

            if (lastMatch.Index > lastEmoteIndex)
            {
                lastEmoteIndex = lastMatch.Index;
                emote = _loc.GetString(emoteKey, ("ent", speaker));
            }

            message = r.Replace(message, string.Empty);
        }

        sanitized = message.Trim();
        return emote is not null;
    }
}
