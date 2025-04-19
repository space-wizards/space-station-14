using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class PirateAccent : IAccent
{
    public string Name { get; } = "Pirate";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        long yarrChance = 1;
        string? pirateWord = null;

        if (attributes.TryGetValue("chance", out var chanceParameter))
            yarrChance = chanceParameter.LongValue!.Value;
        if (attributes.TryGetValue("word", out var wordParameter))
            pirateWord = wordParameter.StringValue;

        var msg = _replacement.ApplyReplacements(message, "pirate");

        if (!_random.Prob(yarrChance))
            return msg;
        //Checks if the first word of the sentence is all caps
        //So the prefix can be allcapped and to not resanitize the captial
        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);

        if (pirateWord != null)
        {
            var locPirateWord = Loc.GetString(pirateWord);
            // Reverse sanitize capital
            if (!firstWordAllCaps)
                msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            else
                locPirateWord = locPirateWord.ToUpper();
            msg = locPirateWord + " " + msg;
        }

        return msg;
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        IoCManager.InjectDependencies(this);
        if (c is PirateAccentComponent comp)
        {
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>() { { "chance", new MarkupParameter((long)comp.YarrChance) }, { "word", new MarkupParameter(_random.Pick(comp.PirateWords)) } });
        }
    }
}
