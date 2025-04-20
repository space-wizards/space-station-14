using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class ScrambledAccent : IAccent
{
    public string Name { get; } = "Scrambled";

    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexLoneI = new(@"(?<=\ )i(?=[\ \.\?]|$)");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
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

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
