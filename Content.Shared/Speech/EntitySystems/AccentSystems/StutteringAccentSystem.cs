using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class StutteringAccentSystem : AccentSystem<StutteringAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Regex of characters to stutter.
    private static readonly Regex Stutter = new(@"[b-df-hj-np-tv-wxyz]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override string Accentuate(Entity<StutteringAccentComponent>? entity, string message)
    {
        var length = message.Length;

        var finalMessage = new StringBuilder();

        string newLetter;

        for (var i = 0; i < length; i++)
        {
            newLetter = message[i].ToString();
            if (Stutter.IsMatch(newLetter) && (entity == null || _random.Prob(entity.Value.Comp.MatchRandomProb)))
            {
                if (entity == null || _random.Prob(entity.Value.Comp.FourRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(entity.Value.Comp.ThreeRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(entity.Value.Comp.CutRandomProb))
                {
                    newLetter = "";
                }
                else
                {
                    newLetter = $"{newLetter}-{newLetter}";
                }
            }

            finalMessage.Append(newLetter);
        }

        return finalMessage.ToString();
    }
}

