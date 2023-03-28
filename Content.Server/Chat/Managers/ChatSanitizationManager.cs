using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using static System.Net.Mime.MediaTypeNames;

namespace Content.Server.Chat.Managers;

public sealed class ChatSanitizationManager : IChatSanitizationManager
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private static readonly Dictionary<string, string> SmileyToEmote = new()
    {
        // I could've done this with regex, but felt it wasn't the right idea.
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
        { ":3", "chatsan-smiles" }, //nope
        { ":S", "chatsan-uncertain" },
        { ":>", "chatsan-grins" },
        { ":<", "chatsan-pouts" },
        { "xD", "chatsan-laughs" },
        { ";-;", "chatsan-cries" },
        { ";_;", "chatsan-cries" },
        { "qwq", "chatsan-cries" },
        { ":u", "chatsan-smiles-smugly" },
        { ":v", "chatsan-smiles-smugly" },
        { ">:i", "chatsan-annoyed" },
        { ":i", "chatsan-sighs" },
        { ":|", "chatsan-sighs" },
        { ":p", "chatsan-stick-out-tongue" },
        { ":b", "chatsan-stick-out-tongue" },
        { "0-0", "chatsan-wide-eyed" },
        { "o-o", "chatsan-wide-eyed" },
        { "o.o", "chatsan-wide-eyed" },
        { "._.", "chatsan-surprised" },
        { ".-.", "chatsan-confused" },
        { "wtf", "chatsan-confused" },
        { "-_-", "chatsan-unimpressed" },
        { "o/", "chatsan-waves" },
        { "^^/", "chatsan-waves" },
        { ":/", "chatsan-uncertain" },
        { ":\\", "chatsan-uncertain" },
        { "lmao", "chatsan-laughs" },
        { "haha", "chatsan-laughs" },
        { "lmfao", "chatsan-laughs" },
        { "lol", "chatsan-laughs" },
        { "lel", "chatsan-laughs" },
        { "kek", "chatsan-laughs" },
        { "o7", "chatsan-salutes" },
        { ";_;7", "chatsan-tearfully-salutes"},
        { "idk", "chatsan-shrugs" }
    };

    private bool _doSanitize;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ChatSanitizerEnabled, x => _doSanitize = x, true);
    }

    public bool TrySanitizeOutSmilies(string input, EntityUid speaker, out string sanitized, [NotNullWhen(true)] out string? emote)
    {
        //Don't do anything if sanitization is disabled
        if (!_doSanitize)
        {
            sanitized = input;
            emote = null;
            return false;
        }

        input = input.TrimEnd();
        string removedWord = "";
        sanitized = input;

        //Go through the dictionary, remove matches and store the last replaced word's replacement
        foreach (KeyValuePair<string, string> entry in SmileyToEmote)
        {
            string pattern = $@"(?<!\w){Regex.Escape(entry.Key)}(?!\w)";
            if (Regex.IsMatch(sanitized, pattern, RegexOptions.IgnoreCase))
            {
                removedWord = entry.Value;
                sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
            }
        }

        //If words were replaced play the emote for the smiley that was deleted and clean up the message
        emote = removedWord;
        if (emote != "")
        {
            sanitized = Regex.Replace(sanitized, @"\s+", " ");
            sanitized = sanitized.Trim();
            //if there is anything left after removing the smiley capitalize the first letter and get rid of spaces in front of punctuation marks
            if (sanitized != "")
            {
                sanitized = Char.ToUpper(sanitized[0]).ToString() + sanitized.Substring(1);
                sanitized = Regex.Replace(sanitized, @"(\s*)(\p{P})", "$2");
            }
            emote = Loc.GetString(emote, ("ent", speaker));
            return true;
        }
        //If no words were replaced return the original message
        sanitized = input;
        emote = null;
        return false;
    }
}
