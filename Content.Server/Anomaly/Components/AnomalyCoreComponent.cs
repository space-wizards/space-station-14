using Content.Server.Anomaly.Effects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component exists for a limited time, and after it expires it modifies the entity, greatly reducing its value and changing its visuals
/// </summary>
[RegisterComponent, Access(typeof(AnomalyCoreSystem))]
public sealed partial class AnomalyCoreComponent : Component
{
    public float AccumulatedFrametime = 3.0f;
    /// <summary>
    ///     How frequently should this price changes, in seconds?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UpdateInterval = 3.0f;

    /// <summary>
    /// Amount of time required for the core to decompose into an inert core
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double TimeToDecay = 300;

    /// <summary>
    /// The moment of core decay. It is set during entity initialization.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DecayMoment;

    /// <summary>
    /// The original value of the entity. The value is linked from the StaticPrice component
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public double OldPrice;

    /// <summary>
    /// The value of the object sought during decaying
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double FuturePrice = 200;

    /// <summary>
    /// Has the nucleus decayed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsDecayed;
}
