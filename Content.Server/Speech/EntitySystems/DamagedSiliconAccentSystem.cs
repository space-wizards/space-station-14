using System.Text;
using Content.Server.PowerCell;
using Content.Server.Speech.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class DamagedSiliconAccentSystem : EntitySystem
{


    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    // Max corruption chance per character due to damage
    private const float MaxCorruption = 0.45f;
    // TotalDamage level that will result in MaxCorruption
    private const int DamageAtMaxCorruption = 300;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamagedSiliconAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, DamagedSiliconAccentComponent component, ref AccentGetEvent args)
    {
        TryComp<DamageableComponent>(uid, out var damageable);
        var damage = damageable?.TotalDamage ?? 0;

        _powerCell.TryGetBatteryFromSlot(uid, out var battery);
        var currentCharge = battery?.CurrentCharge ?? 0.0f;
        var maxCharge = battery?.MaxCharge ?? 1.0f;

        // Charge level from 0 to 1
        var currentChargeLevel = Math.Clamp(currentCharge / maxCharge, 0.0f, 1.0f);

        // Corrupt due to low power (drops characters on longer messages)
        args.Message = CorruptPower(args.Message, currentChargeLevel);
        // Corrupt due to damage (drop, repeat, replace with symbols)
        args.Message = CorruptDamage(args.Message, damage);
    }

    public string CorruptPower(string message, float chargeLevel)
    {
        // The first idxMin characters are SAFE
        const int idxMin = 8;
        // Probability will max at idxMax
        const int idxMax = 40;
        // With no/empty battery, probability to drop will be this value at idxMax
        const float maxDropProbWithEmptyBattery = 0.5f;
        // This will have no effect when charge level is greater than chargeThreshold
        const float chargeThreshold = 0.15f;
        const float probToReplaceWithDot = 0.6f;

        // Fast bails, would not have an effect
        if (chargeLevel > chargeThreshold || message.Length < idxMin)
        {
            return message;
        }

        var outMsg = new StringBuilder();

        var maxDropProb = maxDropProbWithEmptyBattery * (1.0f - chargeLevel / chargeThreshold);

        var idx = -1;
        foreach (var letter in message)
        {
            idx++;
            if (idx < idxMin) // Fast character, no effect
            {
                outMsg.Append(letter);
                continue;
            }

            // use an x^2 interpolation to increase the drop probability until we hit idxMax
            var probToDrop = idx >= idxMax
                ? maxDropProb
                : (float)Math.Pow(((double)idx - idxMin) / (idxMax - idxMin), 2.0) * maxDropProb;
            // Ensure we're in the range for Prob()
            probToDrop = Math.Clamp(probToDrop, 0.0f, 1.0f);

            if (_random.Prob(probToDrop)) // Lose a character
            {
                // Additional chance to change to dot for flavor instead of full drop
                if (_random.Prob(probToReplaceWithDot))
                {
                    outMsg.Append('.');
                }
            }
            else // Character is safe
            {
                outMsg.Append(letter);
            }
        }
        return outMsg.ToString();
    }

    public string CorruptDamage(string message, FixedPoint2 totalDamage)
    {
        var outMsg = new StringBuilder();
        // Linear interpolation of character damage probability
        var damagePercent = Math.Clamp((float)totalDamage / DamageAtMaxCorruption, 0, 1);
        var chanceToCorruptLetter = damagePercent * MaxCorruption;
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
        // If we got here that means we're at least repeating it once
        var res = new StringBuilder($"{letter}{letter}");
        // 25% chance to add another character in the streak
        // (kind of like "exploding dice")
        while (_random.Prob(0.25f))
        {
            res.Append(letter);
        }
        return res.ToString();
    }
}
