using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This component exists for a limited time, and after it expires it modifies the entity, greatly reducing its value and changing its visuals
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnomalyCoreSystem))]
[AutoGenerateComponentState]
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
    [AutoNetworkedField]
    public TimeSpan DecayMoment;

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
    /// Has the core decayed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsDecayed;

    /// <summary>
    /// The amount of GORILLA charges the core has.
    /// Not used when <see cref="IsDecayed"/> is false.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int Charge = 5;
}
