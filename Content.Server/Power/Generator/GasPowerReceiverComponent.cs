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
    [DataField, ViewVariables]
    public float MaxTemperature = 1000.0f;

    /// <summary>
    /// The gas that fuels this generator
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public Gas TargetGas;

    /// <summary>
    /// The amount of gas consumed for operation in magic mode.
    /// </summary>
    [DataField, ViewVariables]
    public float MolesConsumedSec = 1.55975875833f / 4;

    /// <summary>
    /// The amount of kPA "consumed" for operation in pressure mode.
    /// </summary>
    [DataField, ViewVariables]
    public float PressureConsumedSec = 100f;

    /// <summary>
    /// Whether the consumed gas should then be ejected directly into the atmosphere.
    /// </summary>
    [DataField, ViewVariables]
    public bool OffVentGas;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)), ViewVariables(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastProcess = TimeSpan.Zero;

    [DataField, ViewVariables]
    public bool Powered = true;
}
