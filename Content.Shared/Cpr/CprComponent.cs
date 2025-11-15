using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Cpr;

/// <summary>
/// Stores info about the mob's CPR mechanics
/// An entity with this Component can have CPR done on them
/// Has fields that specify how much this mob should be healed when CPR is done on them and the pump sound
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CprComponent : Component
{
    /// <summary>
    /// What damage should be changed when CPR is done - negative for healing
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Change;

    /// <summary>
    /// Healing is applied when the CPR finishes with the mob returning from a critical state - these values should be negative
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier BonusHeal;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public EntityUid? LastCaretaker;

    [DataField, AutoNetworkedField]
    public TimeSpan LastTimeGivenCare = TimeSpan.Zero;
}
