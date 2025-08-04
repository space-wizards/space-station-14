using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class BarkAccentSystem : AccentSystem<BarkAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly List<string> Barks = new()
    {
        " Woof!", " WOOF", " wof-wof",
    };

    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { "ah", "arf" },
        { "Ah", "Arf" },
        { "oh", "oof" },
        { "Oh", "Oof" },
    };

    public override string Accentuate(Entity<BarkAccentComponent>? entity, string message)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message.Replace("!", _random.Pick(Barks))
            .Replace("l", "r").Replace("L", "R");
    }
}

