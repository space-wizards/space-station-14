using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class MobsterAccent : IAccent
{
    public string Name { get; } = "Mobster";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexIng = new(@"(?<=\w\w)(in)g(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexLowerOr = new(@"(?<=\w)o[Rr](?=\w)");
    private static readonly Regex RegexUpperOr = new(@"(?<=\w)O[Rr](?=\w)");
    private static readonly Regex RegexLowerAr = new(@"(?<=\w)a[Rr](?=\w)");
    private static readonly Regex RegexUpperAr = new(@"(?<=\w)A[Rr](?=\w)");
    private static readonly Regex RegexFirstWord = new(@"^(\S+)");
    private static readonly Regex RegexLastWord = new(@"(\S+)$");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        var isBoss = true;
        if (attributes.TryGetValue("isBoss", out var parameter))
            isBoss = parameter.LongValue == 1;

        // Order:
        // Do text manipulations first
        // Then prefix/suffix funnyies

        // direct word replacements
        var msg = _replacement.ApplyReplacements(message, "mobster");

        // thinking -> thinkin'
        // king -> king
        //Uses captures groups to make sure the captialization of IN is kept
        msg = RegexIng.Replace(msg, "$1'");

        // or -> uh and ar -> ah in the middle of words (fuhget, tahget)
        msg = RegexLowerOr.Replace(msg, "uh");
        msg = RegexUpperOr.Replace(msg, "UH");
        msg = RegexLowerAr.Replace(msg, "ah");
        msg = RegexUpperAr.Replace(msg, "AH");

        _random.SetSeed(randomSeed);

        // Prefix
        if (_random.Prob(0.15f))
        {
            //Checks if the first word of the sentence is all caps
            //So the prefix can be allcapped and to not resanitize the captial
            var firstWordAllCaps = !RegexFirstWord.Match(msg).Value.Any(char.IsLower);
            var pick = _random.Next(1, 2);

            // Reverse sanitize capital
            var prefix = Loc.GetString($"accent-mobster-prefix-{pick}");
            if (!firstWordAllCaps)
                msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            else
                prefix = prefix.ToUpper();
            msg = prefix + " " + msg;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        // Suffixes
        if (_random.Prob(0.4f))
        {
            //Checks if the last word of the sentence is all caps
            //So the suffix can be allcapped
            var lastWordAllCaps = !RegexLastWord.Match(msg).Value.Any(char.IsLower);
            var suffix = "";
            if (isBoss)
            {
                var pick = _random.Next(1, 4);
                suffix = Loc.GetString($"accent-mobster-suffix-boss-{pick}");
            }
            else
            {
                var pick = _random.Next(1, 3);
                suffix = Loc.GetString($"accent-mobster-suffix-minion-{pick}");
            }

            if (lastWordAllCaps)
                suffix = suffix.ToUpper();
            msg += suffix;
        }

        return msg;
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        if (c is MobsterAccentComponent comp)
        {
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>() { { "isBoss", new MarkupParameter(comp.IsBoss ? 1 : 0) } });
        }
    }
}
