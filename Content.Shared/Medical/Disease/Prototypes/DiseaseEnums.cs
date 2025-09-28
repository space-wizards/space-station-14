using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Enumeration describing disease transmission vectors.
/// TODO: only the Contact works and Airborne.
/// </summary>
[Flags]
public enum DiseaseSpreadFlags
{
    NonContagious = 0,
    Airborne = 1 << 0,
    Contact = 1 << 1,
    Blood = 1 << 2,
    Special = 1 << 3,
}

/// <summary>
/// Enumeration describing disease stealth behavior flags.
/// TODO: does not work.
/// </summary>
[Flags]
public enum DiseaseStealthFlags
{
    None = 0,
    Hidden = 1 << 0,
    VeryHidden = 1 << 1,
    HiddenTreatment = 1 << 2,
    HiddenStage = 1 << 3,
}

/// <summary>
/// Global configuration for PPE/internals effectiveness used by infection checks.
/// Multipliers are applied multiplicatively to chance (e.g. 0.6 means 40% reduction).
/// </summary>
public static class DiseaseEffectiveness
{
    // Airborne protection
    public const float InternalsMultiplier = 0.25f;

    public static readonly (SlotFlags Slot, float Multiplier)[] AirborneSlots =
    {
        (SlotFlags.MASK, 0.6f),
        (SlotFlags.HEAD, 0.8f),
        (SlotFlags.EYES, 0.9f),
    };

    // Contact protection
    public static readonly (SlotFlags Slot, float Multiplier)[] ContactSlots =
    [
        (SlotFlags.GLOVES, 0.6f),
        (SlotFlags.FEET, 0.8f),
        (SlotFlags.OUTERCLOTHING, 0.8f),
        (SlotFlags.INNERCLOTHING, 0.9f),
    ];
}

/// <summary>
/// How a disease should be represented on HUDs.
/// </summary>
public enum DiseaseIconType
{
    Ill,
    Buff,
    None,
}

public static class DiseaseHud
{
    public static readonly (DiseaseIconType Type, ProtoId<HealthIconPrototype> PrototypeId)[] HudIcons =
    [
        (DiseaseIconType.Ill, "DiseaseIconIll"),
        (DiseaseIconType.Buff, "DiseaseIconBuff"),
        (DiseaseIconType.None, string.Empty),
    ];
}
