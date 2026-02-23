using System.Linq;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class PirateAccentSystem : BaseAccentSystem<PirateAccentComponent>
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    // converts left word when typed into the right word. For example typing you becomes ye.
    public override string Accentuate(string message, Entity<PirateAccentComponent>? entity)
    {
        var msg = _replacement.ApplyReplacements(message, "pirate");

        if (!_random.Prob(entity.HasValue ? entity.Value.Comp.YarrChance : 0.5f))
            return msg;
        //Checks if the first word of the sentence is all caps
        //So the prefix can be allcapped and to not resanitize the captial
        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);

        if (entity.HasValue)
        {
            var pick = _random.Pick(entity.Value.Comp.PirateWords);
            var pirateWord = Loc.GetString(pick);
            // Reverse sanitize capital
            if (!firstWordAllCaps)
                msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            else
                pirateWord = pirateWord.ToUpper();
            msg = pirateWord + " " + msg;
        }

        return msg;
    }
}
