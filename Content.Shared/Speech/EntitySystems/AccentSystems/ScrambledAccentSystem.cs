using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class ScrambledAccentSystem : AccentSystem<ScrambledAccentComponent>
{
    private static readonly Regex RegexLoneI = new(@"(?<=\ )i(?=[\ \.\?]|$)");

    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Accentuate(Entity<ScrambledAccentComponent>? entity, string message)
    {
        var words = message.ToLower().Split();

        if (words.Length < 2)
        {
            var pick = _random.Next(1, 8);
            // If they try to weasel out of it by saying one word at a time we give them this.
            return Loc.GetString($"accent-scrambled-words-{pick}");
        }

        // Scramble the words
        var scrambled = words.OrderBy(x => _random.Next()).ToArray();

        var msg = string.Join(" ", scrambled);

        // First letter should be capital
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        // Capitalize lone i's
        msg = RegexLoneI.Replace(msg, "I");
        return msg;
    }
}

