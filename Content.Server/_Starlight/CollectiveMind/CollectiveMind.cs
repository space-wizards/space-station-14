using Content.Shared.CollectiveMind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Shared.GameTicking;
using Robust.Shared.Utility;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;
using System.Text;
using Content.Shared.Stunnable;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Bed.Sleep;

namespace Content.Server.CollectiveMind;

public sealed partial class CollectiveMind : SharedCollectiveMindSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        //garbling
        SubscribeLocalEvent<CollectiveMindComponent, CollectiveMindMessageAttemptEvent>(OnCollectiveMindMessage, after: [typeof(ReplacementAccentSystem)]);
    }

    private void OnCollectiveMindMessage(Entity<CollectiveMindComponent> ent, ref CollectiveMindMessageAttemptEvent args)
    {
        var uid = ent.Owner;

        if (ent.Comp.CorruptWhenUnconscious)
        {
            //we need to check if the entity is sleeping, or crit
            if (TryComp<MobStateComponent>(uid, out var mobState))
            {
                if (mobState.CurrentState == MobState.Critical || TryComp<SleepingComponent>(uid, out _))
                {
                    args.Message = Corrupt(args.Message, ref ent.Comp);
                }
            }
        }
    }

    private string Corrupt(string message, ref CollectiveMindComponent comp)
    {
        var outMsg = new StringBuilder();
        // Linear interpolation of character damage probability
        foreach (var letter in message)
        {
            if (_random.Prob(comp.CorruptionChanceWhenUnconscious)) // Corrupt!
            {
                outMsg.Append(CorruptLetter(letter));
            }
            else // Safe!
            {
                outMsg.Append(letter);
            }
        }
        return outMsg.ToString();
    }

    private string CorruptLetter(char letter)
    {
        var res = _random.NextDouble();
        return res switch
        {
            < 0.0 => letter.ToString(), // shouldn't be less than 0!
            < 0.5 => CorruptRandom(), // 50% chance to replace with random characters
            < 0.75 => "", // 25% chance to remove character
            < 1.00 => CorruptRepeat(letter), // 25% to repeat the character
            _ => letter.ToString(), // shouldn't be greater than 1!
        };
    }

    private string CorruptRandom()
    {
        //const string punctuation = "\"\\`~!@#$%^&*()_+-={}[]|\\;:<>,.?/";
        const string ran = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return ran[_random.NextByte((byte)ran.Length)].ToString();
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
