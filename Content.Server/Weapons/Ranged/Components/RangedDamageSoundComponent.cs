using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Components;

/// <summary>
/// Plays the specified sound upon receiving damage of that type.
/// </summary>
[RegisterComponent]
public sealed partial class RangedDamageSoundComponent : Component
{
    // TODO: Limb damage changing sound type.

    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified group.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, SoundSpecifier>? SoundGroups;

    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified type.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, SoundSpecifier>? SoundTypes;
}
