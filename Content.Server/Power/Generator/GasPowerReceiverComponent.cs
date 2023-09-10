using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for providing gas power to machinery.
/// </summary>
[RegisterComponent, Access(typeof(GasPowerReceiverSystem))]
public sealed partial class GasPowerReceiverComponent : Component
{
    /// <summary>
    /// Past this temperature we assume we're in reaction mass mode and not magic mode.
    /// </summary>
    [DataField("maxTemperature"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxTemperature = 1000.0f;

    /// <summary>
    /// The gas that fuels this generator
    /// </summary>
    [DataField("targetGas", required: true), ViewVariables(VVAccess.ReadWrite)]
    public Gas TargetGas;

    /// <summary>
    /// The amount of gas consumed for operation in magic mode.
    /// </summary>
    [DataField("molesConsumedSec"), ViewVariables(VVAccess.ReadWrite)]
    public float MolesConsumedSec = 1.55975875833f / 4;

    /// <summary>
    /// The amount of kPA "consumed" for operation in pressure mode.
    /// </summary>
    [DataField("pressureConsumedSec"), ViewVariables(VVAccess.ReadWrite)]
    public float PressureConsumedSec = 100f;

    /// <summary>
    /// Whether the consumed gas should then be ejected directly into the atmosphere.
    /// </summary>
    [DataField("offVentGas"), ViewVariables(VVAccess.ReadWrite)]
    public bool OffVentGas;

    [DataField("lastProcess", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastProcess = TimeSpan.Zero;

    [DataField("powered"), ViewVariables(VVAccess.ReadWrite)]
    public bool Powered = true;
}
