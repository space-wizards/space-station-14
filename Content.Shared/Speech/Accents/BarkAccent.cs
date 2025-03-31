using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class BarkAccent : IAccent
{
    public string Name { get; } = "Bark";

    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly List<string> Barks = new()
    {
        " Woof!", " WOOF", " wof-wof"
    };

    private static readonly Dictionary<string, string> SpecialWords = new()
    {
        { "ah", "arf" },
        { "Ah", "Arf" },
        { "oh", "oof" },
        { "Oh", "Oof" },
    };

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        _random.SetSeed(randomSeed);

        return message.Replace("!", _random.Pick(Barks))
            .Replace("l", "r").Replace("L", "R");
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}

