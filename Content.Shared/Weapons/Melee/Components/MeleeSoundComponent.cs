using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// Plays the specified sound upon receiving damage of the specified type.
/// </summary>
[RegisterComponent]
public sealed partial class MeleeSoundComponent : Component
{
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

    /// <summary>
    /// Sound that plays if no damage is done.
    /// </summary>
    [DataField]
    public SoundSpecifier? NoDamageSound;
}
