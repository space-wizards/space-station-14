using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CPR;

/// <summary>
/// Stores info about the mob's CPR mechanics
/// An entity with this Component can have CPR done on them
/// Has fields that specify how much this mob should be healed when CPR is done on them and the pump sound
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CPRComponent : Component
{
    /// <summary>
    /// What damage should be changed when CPR is done - negative for healing
    /// </summary>
    [DataField]
    public DamageSpecifier Change = default!;
    /// <summary>
    /// Healing is applied when the CPR finishes with the mob returning from a critical state - these values should be negative
    /// </summary>
    [DataField]
    public DamageSpecifier BonusHeal = default!;

    [DataField]
    public SoundSpecifier? Sound = null;
}
