using Content.Shared.Damage;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.RussStation.Surgery.Effects;

/// <summary>
/// Surgery effect: heals damage on completion (used by Tend Wounds).
/// Note: For Tend Wounds, healing is done per-step via the repeatable hemostat step,
/// so this effect class exists for future use or additional completion healing.
/// </summary>
[DataDefinition]
public sealed partial class HealDamageEffect : ISurgeryEffect
{
    [DataField]
    public DamageSpecifier? Healing;
}
