using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TendingSystem))]
public sealed partial class TendingComponent : Component
{
    /// <summary>
    /// Damage specifier to apply when this tends
    /// </summary>
    [DataField(readOnly: true)]
    public DamageSpecifier? Damage;

    /// <summary>
    /// What wounds can be tended
    /// </summary>
    [DataField]
    public EntityWhitelist? WoundWhitelist;

    /// <summary>
    /// What wounds can not be tended
    /// </summary>
    [DataField]
    public EntityWhitelist? WoundBlacklist;

    /// <summary>
    /// How long the doafter takes
    /// </summary>
    [DataField]
    public float Delay = 1f;

    /// <summary>
    /// The time penalty for self-tending wounds
    /// </summary>
    [DataField]
    public float SelfTendPenaltyModifier = 5f;

    [DataField]
    public SoundSpecifier? TendingBeginSound;

    [DataField]
    public SoundSpecifier? TendingEndSound;

    [DataField]
    public LocId NothingToTend = "tendable-nothing-to-tend";

    [DataField]
    public LocId NothingToTendRepeat = "tendable-nothing-to-tend-repeat";

    [DataField]
    public LocId UsedUp = "tendable-used-up";

    [DataField]
    public LocId SelfPopup = "tendable-self-tending";

    [DataField]
    public LocId UserPopup = "tendable-user-tending";

    [DataField]
    public LocId OtherPopup = "tendable-other-tending";
}

[Serializable, NetSerializable]
public sealed partial class TendingDoAfterEvent : SimpleDoAfterEvent;
