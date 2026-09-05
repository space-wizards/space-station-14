using System.Text;
using Content.Server.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;

using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class DamagedSiliconAccentSystem : SharedDamagedSiliconAccentSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DestructibleSystem _destructibleSystem = default!;

    protected override string CorruptDamage(string message, FixedPoint2 totalDamage, Entity<DamagedSiliconAccentComponent> ent)
    {
        var outMsg = new StringBuilder();

        // If this is not specified, use the Destructible threshold for destruction or breakage
        var damageAtMaxCorruption = ent.Comp.DamageAtMaxCorruption;
        if (damageAtMaxCorruption is null)
        {
            if (!TryComp<DestructibleComponent>(ent, out var destructible))
                return message;

            damageAtMaxCorruption = _destructibleSystem.DestroyedAt(ent, destructible);
        }

        // Linear interpolation of character damage probability
        var damagePercent = Math.Clamp((float)totalDamage / (float)damageAtMaxCorruption, 0, 1);
        var chanceToCorruptLetter = damagePercent * ent.Comp.MaxDamageCorruption;
        foreach (var letter in message)
        {
            if (_random.Prob(chanceToCorruptLetter)) // Corrupt!
            {
                outMsg.Append(CorruptLetterDamage(letter));
            }
            else // Safe!
            {
                outMsg.Append(letter);
            }
        }
        return outMsg.ToString();
    }

    private string CorruptLetterDamage(char letter)
    {
        var res = _random.NextDouble();
        return res switch
        {
            < 0.0 => letter.ToString(), // shouldn't be less than 0!
            < 0.5 => CorruptPunctuize(), // 50% chance to replace with random punctuation
            < 0.75 => "", // 25% chance to remove character
            < 1.00 => CorruptRepeat(letter), // 25% to repeat the character
            _ => letter.ToString(), // shouldn't be greater than 1!
        };
    }

    private string CorruptPunctuize()
    {
        const string punctuation = "\"\\`~!@#$%^&*()_+-={}[]|\\;:<>,.?/";
        return punctuation[_random.NextByte((byte)punctuation.Length)].ToString();
    }

    private string CorruptRepeat(char letter)
    {
        // 25% chance to add another character in the streak
        // (kind of like "exploding dice")
        // Solved numerically in closed form for streaks of bernoulli variables with p = 0.25
        // Can calculate for different p using python function:
        /*
         *     def prob(streak, p):
         *         if streak == 0:
         *             return scipy.stats.binom(streak+1, p).pmf(streak)
         *         return prob(streak-1) * p
         *     def prob_cum(streak, p=.25):
         *         return np.sum([prob(i, p) for i in range(streak+1)])
         */
        var numRepeats = _random.NextDouble() switch
        {
            < 0.75000000 => 2,
            < 0.93750000 => 3,
            < 0.98437500 => 4,
            < 0.99609375 => 5,
            < 0.99902344 => 6,
            < 0.99975586 => 7,
            < 0.99993896 => 8,
            < 0.99998474 => 9,
            _ => 10,
        };
        return new string(letter, numRepeats);
    }
}
