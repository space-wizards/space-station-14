using Robust.Shared.Random;

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

    public string Accentuate(string message, int randomSeed)
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
}

