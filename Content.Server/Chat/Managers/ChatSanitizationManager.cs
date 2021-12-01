using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;


namespace Content.Server.Chat.Managers;

public class ChatSanitizationManager : IChatSanitizationManager
{
    [Dependency] private IConfigurationManager _configurationManager = default!;

    private static readonly Dictionary<string, string> SmilelyToEmote = new()
    {
        // I could've done this with regex, but felt it wasn't the right idea.
        { ":)", "smiles" },
        { ":]", "smiles" },
        { "(:", "smiles" },
        { "[:", "smiles" },
        { ":(", "frowns" },
        { "):", "frowns" },
        { "]:", "frowns" },
        { ":[", "frowns" },
        { ":D", "smiles widely" },
        { "D:", "frowns deeply" },
        { ":O", "looks surprised" },
        { ":3", "smiles" }, //nope
        { ":S", "looks uncertain" },
        { ":>", "grins" },
        { ":<", "pouts" }, //maybe smth better for this
        { "xD", "laughs" },
        { ";-;", "cries" },
        { ";_;", "cries" },
    };

    public bool TrySanitizeOutSmilies(string input, out string sanitized, [NotNullWhen(true)] out string? emote)
    {
        if (!_configurationManager.GetCVar(CCVars.ChatSanitizerEnabled))
        {
            sanitized = input;
            emote = null;
            return false;
        }

        input = input.TrimEnd();

        foreach (var smiley in SmilelyToEmote.Keys)
        {
            if (input.EndsWith(smiley, true, CultureInfo.InvariantCulture))
            {
                var idx = input.LastIndexOf(smiley, StringComparison.Ordinal);
                sanitized = input.Remove(idx).TrimEnd();
                emote = SmilelyToEmote[smiley];
                return true;
            }
        }

        sanitized = input;
        emote = null;
        return false;
    }
}
