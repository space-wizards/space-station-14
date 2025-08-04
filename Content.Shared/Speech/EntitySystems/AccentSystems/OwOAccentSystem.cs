using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class OwOAccentSystem : AccentSystem<OwOAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly List<string> Faces = new()
    {
        " (•`ω´•)", " ;;w;;", " owo", " UwU", " >w<", " ^w^"
    };

    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { "you", "wu" },
    };

    public override string Accentuate(Entity<OwOAccentComponent>? entity, string message)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message.Replace("!", _random.Pick(Faces))
            .Replace("r", "w").Replace("R", "W")
            .Replace("l", "w").Replace("L", "W");
    }
}

