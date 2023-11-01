using Content.Server.Anomaly.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, Access(typeof(AnomalyCoreSystem))]
public sealed partial class AnomalyCoreComponent : Component
{
    /// <summary>
    /// Amount of time required for the core to decompose into an inert core
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double TimeToDecay = 600;
    /// <summary>
    /// The moment of core decay. It is set during entity initialization.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DecayMoment;
    /// <summary>
    /// Has the core decayed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsDecayed;

    /// <summary>
    /// Amount of time required for the core to collapse after interaction
    /// corresponds to the length of the <see cref="ChargingSound"></see>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double TimeToCollapse = 3;
    /// <summary>
    /// The moment the core explodes after activation.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CollapseMoment;
    /// <summary>
    /// Core collapsing now?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsCollapsing;

    /// <summary>
    /// The starting value of the entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double StartPrice = 10000;

    /// <summary>
    /// The value of the object sought during decaying
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double EndPrice = 200;

    /// <summary>
    /// the sound made at the onset of core collapse
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ChargingSound = new SoundPathSpecifier("/Audio/Effects/anomaly_core_charge.ogg");

    /// <summary>
    /// the sound of the core collapsing.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier CollapseSound = new SoundCollectionSpecifier("RadiationPulse");
}
