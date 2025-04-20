using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class StutteringAccent : IAccent
{
    public string Name { get; } = "Stuttering";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    // Regex of characters to stutter.
    private static readonly Regex Stutter = new(@"[b-df-hj-np-tv-wxyz]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        var matchRandomProb = 1f;
        var fourRandomProb = 1f;
        var threeRandomProb = 1f;
        var cutRandomProb = 1f;
        if (attributes.TryGetValue("matchRandomProb", out var matchParameter))
            matchRandomProb = matchParameter.LongValue!.Value;
        if (attributes.TryGetValue("fourRandomProb", out var fourParameter))
            fourRandomProb = fourParameter.LongValue!.Value;
        if (attributes.TryGetValue("threeRandomProb", out var threeParameter))
            threeRandomProb = threeParameter.LongValue!.Value;
        if (attributes.TryGetValue("cutRandomProb", out var cutParameter))
            cutRandomProb = cutParameter.LongValue!.Value;
        var length = message.Length;

        var finalMessage = new StringBuilder();

        string newLetter;

        for (var i = 0; i < length; i++)
        {
            newLetter = message[i].ToString();
            if (Stutter.IsMatch(newLetter) && _random.Prob(matchRandomProb))
            {
                if (_random.Prob(fourRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(threeRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(cutRandomProb))
                {
                    newLetter = "";
                }
                else
                {
                    newLetter = $"{newLetter}-{newLetter}";
                }
            }

            finalMessage.Append(newLetter);
        }

        return finalMessage.ToString();
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        if (c is StutteringAccentComponent comp)
        {
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>()
                {
                    { "matchRandomProb", new MarkupParameter((long)comp.MatchRandomProb) },
                    { "fourRandomProb", new MarkupParameter((long)comp.FourRandomProb) },
                    { "threeRandomProb", new MarkupParameter((long)comp.ThreeRandomProb) },
                    { "cutRandomProb", new MarkupParameter((long)comp.CutRandomProb) },
                });
        }
    }
}
