using System.Collections.Frozen;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class BarkAccentSystem : RelayAccentSystem<BarkAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly IReadOnlyList<string> Barks =
    [
        " Woof!", " WOOF", " wof-wof",
    ];

    private static readonly FrozenDictionary<string, string> SpecialWords =
        new Dictionary<string, string>
        {
            { "ah", "arf" },
            { "Ah", "Arf" },
            { "oh", "oof" },
            { "Oh", "Oof" },
        }.ToFrozenDictionary();

    public override string Accentuate(string message, Entity<BarkAccentComponent>? ent = null)
    {
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        return message.Replace("!", random.Pick(Barks))
            .Replace("l", "r")
            .Replace("L", "R");
    }
}
