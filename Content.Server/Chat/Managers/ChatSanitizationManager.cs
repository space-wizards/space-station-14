using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

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
        { ":u", "chatsan-smug" },
        { ":v", "chatsan-smug" },
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
        { "-_-", "chatsan-unimpressed" },
        { "o/", "chatsan-waves" },
        { "^^/", "chatsan-waves" },
        { ":/", "chatsan-uncertain" },
        { ":\\", "chatsan-uncertain" },
        { "lmao", "chatsan-laughs" },
        { "lmao.", "chatsan-laughs" },
        { "lol", "chatsan-laughs" },
        { "lol.", "chatsan-laughs" },
        { "lel", "chatsan-laughs" },
        { "lel.", "chatsan-laughs" },
        { "kek", "chatsan-laughs" },
        { "kek.", "chatsan-laughs" },
        { "o7", "chatsan-salutes" },
        { ";_;7", "chatsan-tearfully-salutes"},
        { "idk", "chatsan-shrugs" },
        // Asterisk emotes
        { "*blink", "chatsan-blinks" },
        { "*blinks", "chatsan-blinks" },
        { "*blush", "chatsan-blushes" },
        { "*blushes", "chatsan-blushes" },
        { "*bow", "chatsan-bows" },
        { "*bows", "chatsan-bows" },
        { "*chuckle", "chatsan-chuckles" },
        { "*chuckles", "chatsan-chuckles" },
        { "*clap", "chatsan-claps" },
        { "*claps", "chatsan-claps" },
        { "*cry", "chatsan-cries" },
        { "*cries", "chatsan-cries" },
        { "*cross", "chatsan-crosses" },
        { "*crosses", "chatsan-crosses" },
        { "*cough", "chatsan-coughs" },
        { "*coughs", "chatsan-coughs" },
        { "*dance", "chatsan-dances" },
        { "*dances", "chatsan-dances" },
        { "*deathgasp", "chatsan-deathgasp" },
        { "*deathgasps", "chatsan-deathgasp" },
        { "*eyebrow", "chatsan-eyebrow" },
        { "*faint", "chatsan-faints" },
        { "*faints", "chatsan-faints" },
        { "*flip", "chatsan-flips" },
        { "*flips", "chatsan-flips" },
        { "*frown", "chatsan-frowns" },
        { "*frowns", "chatsan-frowns" },
        { "*gag", "chatsan-gags" },
        { "*gags", "chatsan-gags" },
        { "*gasp", "chatsan-gasps" },
        { "*gasps", "chatsan-gasps" },
        { "*giggle", "chatsan-giggles" },
        { "*giggles", "chatsan-giggles" },
        { "*giggle_m", "chatsan-giggles-silently" },
        { "*giggles_m", "chatsan-giggles-silently" },
        { "*grimace", "chatsan-grimaces" },
        { "*grimaces", "chatsan-grimaces" },
        { "*grin", "chatsan-grins" },
        { "*laugh", "chatsan-laughs" },
        { "*laughs", "chatsan-laughs" },
        { "*laugh_m", "chatsan-laughs-silently" },
        { "*laughs_m", "chatsan-laughs-silently" },
        { "*mumble", "chatsan-mumbles" },
        { "*mumbles", "chatsan-mumbles" },
        { "*nod", "chatsan-nods" },
        { "*nods", "chatsan-nods" },
        { "*pale", "chatsan-pale" },
        { "*pouts", "chatsan-pouts" },
        { "*raise", "chatsan-raises" },
        { "*raises", "chatsan-raises" },
        { "*salute", "chatsan-salutes" },
        { "*salutes", "chatsan-salutes" },
        { "*salute_t", "chatsan-tearfully-salutes" },
        { "*scream", "chatsan-screams" },
        { "*screams", "chatsan-screams" },
        { "*scream_m", "chatsan-screams-silently" },
        { "*screams_m", "chatsan-screams-silently" },
        { "*shake", "chatsan-shakes-head" },
        { "*shakes", "chatsan-shakes-head" },
        { "*shake_r", "chatsan-shakes-head-rapidly" },
        { "*shakes_r", "chatsan-shakes-head-rapidly" },
        { "*shiver", "chatsan-shivers" },
        { "*shivers", "chatsan-shivers" },
        { "*shrug", "chatsan-shrugs" },
        { "*shrugs", "chatsan-shrugs" },
        { "*sigh", "chatsan-sighs" },
        { "*sighs", "chatsan-sighs" },
        { "*sit", "chatsan-sits" },
        { "*sits", "chatsan-sits" },
        { "*smile", "chatsan-smiles" },
        { "*smiles", "chatsan-smiles" },
        { "*smug", "chatsan-smug" },
        { "*smugs", "chatsan-smug" },
        { "*snap", "chatsan-snaps" },
        { "*snaps", "chatsan-snaps" },
        { "*sneeze", "chatsan-sneezes" },
        { "*sneezes", "chatsan-sneezes" },
        { "*snore", "chatsan-snores" },
        { "*snores", "chatsan-snores" },
        { "*spin", "" }, // TBI
        { "*stretch", "chatsan-stretches" },
        { "*stretches", "chatsan-stretches" },
        { "*sulk", "chatsan-sulks" },
        { "*sulks", "chatsan-sulks" },
        { "*surrender", "chatsan-surrenders" },
        { "*surrenders", "chatsan-surrenders" },
        { "*sway", "chatsan-sways" },
        { "*sways", "chatsan-sways" },
        { "*tilt", "chatsan-tilts" },
        { "*tilts", "chatsan-tilts" },
        { "*tremble", "chatsan-trembles" },
        { "*trembles", "chatsan-trembles" },
        { "*twitch", "chatsan-twitches" },
        { "*twitches", "chatsan-twitches" },
        { "*twitch_s", "chatsan-twitches-slight" },
        { "*whimper", "chatsan-whimpers" },
        { "*whimpers", "chatsan-whimpers" },
        { "*whimper_m", "chatsan-whimpers-silently" },
        { "*whimpers_m", "chatsan-whimpers-silently" },
        { "*wave", "chatsan-waves" },
        { "*waves", "chatsan-waves" },
        { "*wsmile", "chatsan-smiles-weakly" },
        { "*wsmiles", "chatsan-smiles-weakly" },
        { "*yawn", "chatsan-yawns" },
        { "*yawns", "chatsan-yawns" }
    };

    private bool _doSanitize;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ChatSanitizerEnabled, x => _doSanitize = x, true);
    }

    public bool TrySanitizeOutSmilies(string input, EntityUid speaker, out string sanitized, [NotNullWhen(true)] out string? emote)
    {
        if (!_doSanitize)
        {
            sanitized = input;
            emote = null;
            return false;
        }

        input = input.TrimEnd();

        foreach (var (smiley, replacement) in SmileyToEmote)
        {
            if (input.EndsWith(smiley, true, CultureInfo.InvariantCulture))
            {
                sanitized = input.Remove(input.Length - smiley.Length).TrimEnd();
                emote = Loc.GetString(replacement, ("ent", speaker));
                return true;
            }
        }

        sanitized = input;
        emote = null;
        return false;
    }
}
