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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeToDecay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The moment of core decay. It is set during entity initialization.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DecayMoment;

    /// <summary>
    /// The original value of the entity. The value is used from the StaticPrice component
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public double OldPrice;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double FuturePrice = 200;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsDecayed;
}
