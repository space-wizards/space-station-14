using Robust.Shared.Random;

namespace Content.Shared.Speech.Accents;

public sealed class OwOAccent : IAccent
{
    public string Name { get; } = "OwO";

    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly List<string> Faces = new()
    {
        " (•`ω´•)", " ;;w;;", " owo", " UwU", " >w<", " ^w^"
    };

    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { "you", "wu" },
    };

    public string Accentuate(string message, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        _random.SetSeed(randomSeed);

        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message.Replace("!", _random.Pick(Faces))
            .Replace("r", "w").Replace("R", "W")
            .Replace("l", "w").Replace("L", "W");
    }
}
