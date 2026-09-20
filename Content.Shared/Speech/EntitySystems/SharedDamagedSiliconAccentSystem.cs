using System.Text;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public abstract partial class SharedDamagedSiliconAccentSystem : RelayAccentSystem<DamagedSiliconAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    protected override Type[] AccentAfter => [typeof(ReplacementAccentSystem)];
    protected override Type[] RelayAccentAfter => [typeof(ReplacementAccentSystem)];

    public override string Accentuate(string message, Entity<DamagedSiliconAccentComponent>? ent = null)
    {
        if (ent == null)
            return message;

        var uid = ent.Value.Owner;
        if (ent.Value.Comp.EnableChargeCorruption)
        {
            var currentChargeLevel = 0.0f;
            if (ent.Value.Comp.OverrideChargeLevel.HasValue)
            {
                currentChargeLevel = ent.Value.Comp.OverrideChargeLevel.Value;
            }
            else if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                currentChargeLevel = _battery.GetChargeLevel(battery.Value.AsNullable());
            }
            currentChargeLevel = Math.Clamp(currentChargeLevel, 0.0f, 1.0f);
            // Corrupt due to low power (drops characters on longer messages)
            message = CorruptPower(message, currentChargeLevel, ent.Value);
        }

        if (ent.Value.Comp.EnableDamageCorruption)
        {
            var damage = FixedPoint2.Zero;
            if (ent.Value.Comp.OverrideTotalDamage.HasValue)
            {
                damage = ent.Value.Comp.OverrideTotalDamage.Value;
            }
            else if (TryComp<DamageableComponent>(uid, out var damageable))
            {
                damage = _damageable.GetTotalDamage((uid, damageable));
            }
            // Corrupt due to damage (drop, repeat, replace with symbols)
            message = CorruptDamage(message, damage, ent.Value);
        }

        return message;
    }

    protected virtual string CorruptDamage(string message, FixedPoint2 totalDamage, Entity<DamagedSiliconAccentComponent> ent)
    {
        return message;
    }

    /// <summary>
    /// Corrupts a message based on the entity's charge level.
    /// </summary>
    public string CorruptPower(string message, float chargeLevel, Entity<DamagedSiliconAccentComponent> ent)
    {
        // The first idxMin characters are SAFE
        var idxMin = ent.Comp.StartPowerCorruptionAtCharIdx;
        // Probability will max at idxMax
        var idxMax = ent.Comp.MaxPowerCorruptionAtCharIdx;

        // Fast bails, would not have an effect
        if (chargeLevel > ent.Comp.ChargeThresholdForPowerCorruption || message.Length < idxMin)
        {
            return message;
        }

        var outMsg = new StringBuilder();

        var maxDropProb = ent.Comp.MaxDropProbFromPower * (1.0f - chargeLevel / ent.Comp.ChargeThresholdForPowerCorruption);

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

            var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
            if (random.Prob(probToDrop)) // Lose a character
            {
                // Additional chance to change to dot for flavor instead of full drop
                if (random.Prob(ent.Comp.ProbToCorruptDotFromPower))
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
}
