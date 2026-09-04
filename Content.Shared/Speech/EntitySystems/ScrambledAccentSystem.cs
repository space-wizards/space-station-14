using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class ScrambledAccentSystem : RelayAccentSystem<ScrambledAccentComponent>
{
    private static readonly Regex RegexLoneI = new(@"(?<=\ )i(?=[\ \.\?]|$)");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override string Accentuate(string message, Entity<ScrambledAccentComponent>? ent = null)
    {
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        var words = message.ToLower().Split();

        if (words.Length < 2)
        {
            var pick = random.Next(1, 8);
            // If they try to weasel out of it by saying one word at a time we give them this.
            return Loc.GetString($"accent-scrambled-words-{pick}");
        }

        // Scramble the words
        var scrambled = words.OrderBy(_ => random.Next()).ToArray();

        var msg = string.Join(" ", scrambled);

        // First letter should be capital
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        // Capitalize lone i's
        msg = RegexLoneI.Replace(msg, "I");
        return msg;
    }
}
