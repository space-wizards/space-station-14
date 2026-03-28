using Content.Shared.Damage;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.RussStation.Surgery.Effects;

/// <summary>
/// Surgery effect that heals damage when a procedure completes.
/// </summary>
[DataDefinition]
public sealed partial class HealDamageEffect : ISurgeryEffect
{
    [DataField]
    public DamageSpecifier? Healing;
}
