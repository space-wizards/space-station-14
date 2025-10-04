using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CprSystem))]
public sealed partial class CprTargetComponent : Component
{
    /// <summary>
    /// The status effect to apply when CPR is performed
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Effect;

    /// <summary>
    /// How long the CPR effect lasts
    /// </summary>
    [DataField(required: true)]
    public TimeSpan EffectDuration;

    /// <summary>
    /// How long the doafter takes
    /// </summary>
    [DataField(required: true)]
    public TimeSpan DoAfterDuration;

    /// <summary>
    /// Which wound can be caused by CPR
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Wound;

    /// <summary>
    /// How likely is the wound to happen when CPR happens?
    /// </summary>
    [DataField(required: true)]
    public double WoundProbability;

    [DataField]
    public LocId WoundPopup = "cpr-wound-caused";

    [DataField]
    public LocId UserPopup = "cpr-target-started-user";

    [DataField]
    public LocId OtherPopup = "cpr-target-started-others";
}

[Serializable, NetSerializable]
public sealed partial class CprDoAfterEvent : SimpleDoAfterEvent;
