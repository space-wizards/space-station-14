using System.Linq;
using Robust.Shared.Random;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class PirateAccentSystem : AccentSystem<PirateAccentComponent>
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    // converts left word when typed into the right word. For example typing you becomes ye.
    public override string Accentuate(Entity<PirateAccentComponent>? entity, string message)
    {
        var msg = _replacement.ApplyReplacements(message, "pirate");

        if (entity == null || !_random.Prob(entity.Value.Comp.YarrChance))
            return msg;
        //Checks if the first word of the sentence is all caps
        //So the prefix can be allcapped and to not resanitize the captial
        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);

        var pick = _random.Pick(entity.Value.Comp.PirateWords);
        var pirateWord = Loc.GetString(pick);
        // Reverse sanitize capital
        if (!firstWordAllCaps)
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        else
            pirateWord = pirateWord.ToUpper();
        msg = pirateWord + " " + msg;

        return msg;
    }
}
