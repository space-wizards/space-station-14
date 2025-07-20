// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class InfectionDeadComponent : Component
{
    public InfectionDeadComponent(InfectionDeadStrainData sd)
    {
        StrainData = sd;
    }

    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("damageDuration", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DamageDuration = TimeSpan.FromSeconds(60);

    [DataField("nextDamageTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDamageTime = TimeSpan.Zero;

    [DataField]
    public InfectionDeadStrainData StrainData = new InfectionDeadStrainData();
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class InfectionDeadStrainData : ReagentData
{
    public Color SkinColor = new(0.8f, 0.72f, 0.73f);

    [DataField]
    public float DamageMulty;

    [DataField]
    public float StaminaMulty;

    [DataField]
    public float HpMulty;

    [DataField]
    public float SpeedMulty;
    public VirusEffects Effects;
    public InfectionDeadStrainData()
    {
        DamageMulty = 1f;
        StaminaMulty = 1f;
        HpMulty = 1f;
        SpeedMulty = 1f;
        Effects = new VirusEffects();
    }
    public override bool Equals(ReagentData? other)
    {
        if (other is not InfectionDeadStrainData o)
            return false;

        return DamageMulty == o.DamageMulty &&
               StaminaMulty == o.StaminaMulty &&
               HpMulty == o.HpMulty &&
               SpeedMulty == o.SpeedMulty &&
               Effects == o.Effects &&
               SkinColor.Equals(o.SkinColor);
    }

    public override ReagentData Clone()
    {
        return new InfectionDeadStrainData
        {
            DamageMulty = this.DamageMulty,
            StaminaMulty = this.StaminaMulty,
            HpMulty = this.HpMulty,
            SpeedMulty = this.SpeedMulty,
            Effects = this.Effects,
            SkinColor = this.SkinColor
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DamageMulty, StaminaMulty, HpMulty, SpeedMulty, Effects, SkinColor);
    }
}

[Flags]
public enum VirusEffects : ushort
{
    None = 0,
    NightVision = 1 << 0,
    EmitGas = 2 << 1,
    NoSlip = 3 << 3,
    Explosion = 4 << 4,
    Invisability = 5 << 5,
    Pulling = 6 << 6,
    StunAttack = 7 << 7,
    Dash = 8 << 8,
    Insulated = 9 << 9,
    Vampirism = 10 << 10,
    Incurability = 11 << 11
}

public static class VirusEffectsConditions
{
    public static float MaxDamageMulty = 3f;
    public static float MinDamageMulty = 0.5f;
    public static float MaxStaminaMulty = 3f;
    public static float MinStaminaMulty = 0.5f;
    public static float MaxHpMulty = 3f;
    public static float MinHpMulty = 0.5f;
    public static float MaxSpeedMulty = 2f;
    public static float MinSpeedMulty = 0.5f;
    public static DamageSpecifier HealingOnBite = new()
    {
        DamageDict = new()
        {
            { "Blunt", -8 },
            { "Slash", -8 },
            { "Piercing", -8 },
            { "Cold", -8 },
            { "Heat", -8 }
        }
    };
    public static readonly Dictionary<VirusEffects, float> Weights = new()
    {
        { VirusEffects.NightVision, 1f},
        { VirusEffects.EmitGas, 0.8f},
        { VirusEffects.NoSlip, 0.5f},
        { VirusEffects.Explosion, 0.1f},
        { VirusEffects.Pulling, 1f},
        { VirusEffects.StunAttack, 0.5f},
        { VirusEffects.Dash, 0.1f},
        { VirusEffects.Insulated, 0.3f},
        { VirusEffects.Vampirism, 0.5f},
        { VirusEffects.Invisability, 0.1f},
        { VirusEffects.Incurability, 0.3f}
    };

    /// <summary>
    /// Проверяет, содержит ли эффект указанный эффект.
    /// </summary>
    public static bool HasEffect(VirusEffects currentEffects, VirusEffects effect)
    {
        return (currentEffects & effect) == effect;
    }

    /// <summary>
    /// Добавляет эффект к текущим эффектам.
    /// </summary>
    public static VirusEffects AddEffect(VirusEffects currentEffects, VirusEffects effect)
    {
        return currentEffects | effect;
    }

    /// <summary>
    /// Убирает эффект из текущих эффектов.
    /// </summary>
    public static VirusEffects RemoveEffect(VirusEffects currentEffects, VirusEffects effect)
    {
        return currentEffects & ~effect;
    }

    /// <summary>
    /// Получает список активных эффектов.
    /// </summary>
    public static IEnumerable<VirusEffects> GetActiveEffects(VirusEffects effects)
    {
        foreach (VirusEffects effect in Enum.GetValues(typeof(VirusEffects)))
        {
            if (effect == VirusEffects.None)
                continue;

            if (HasEffect(effects, effect))
                yield return effect;
        }
    }
}

[ByRefEvent]
public readonly record struct InfectionDeadSymptomsEvent();

[ByRefEvent]
public readonly record struct InfectionNecroficationEvent();
