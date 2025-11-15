using Content.Shared.Power.EntitySystems;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Components;

/// <summary>
/// Predicted equivalent to <see cref="BatteryComponent"/>.
/// Use this for electrical power storages that only have a constant charge rate or instantaneous power draw.
/// Devices being directly charged by the power network do not fulfill that requirement as their power supply ramps up over time.
/// </summary>
/// <remarks>
/// We cannot simply network <see cref="BatteryComponent"/> since it would get dirtied every single tick when it updates.
/// This component solves this by requiring a constant charge rate and having the client infer the current charge from the rate
/// and the timestamp the charge was last networked at. This can possibly be expanded in the future by adding a second time derivative.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(PredictedBatterySystem))]
public sealed partial class PredictedBatteryComponent : Component
{
    /// <summary>
    /// Maximum charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    [GuidebookData]
    public float MaxCharge;

    /// <summary>
    /// The price per one joule. Default is 1 speso for 10kJ.
    /// </summary>
    [DataField]
    public float PricePerJoule = 0.0001f;

    /// <summary>
    /// Time stamp of the last networked update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField, ViewVariables]
    public TimeSpan LastUpdate = TimeSpan.Zero;

    /// <summary>
    /// The intial charge to be set on map init.
    /// </summary>
    [DataField]
    public float StartingCharge;

    /// <summary>
    /// The charge at the last update in joules (i.e. watt seconds).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float LastCharge;

    /// <summary>
    /// The current charge rate in watt.
    /// </summary>
    /// <remarks>
    /// Not a datafield as this is only cached and recalculated on component startup.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public float ChargeRate;

    /// <summary>
    /// The current charge state of the battery.
    /// Used to track state changes for raising <see cref="PredictedBatteryStateChangedEvent"/>.
    /// </summary>
    /// <remarks>
    /// Not a datafield as this is only cached and recalculated in an update loop.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public BatteryState State = BatteryState.Neither;
}

/// <summary>
/// Charge level status of the battery.
/// </summary>
[Serializable, NetSerializable]
public enum BatteryState : byte
{
    /// <summary>
    /// Full charge.
    /// </summary>
    Full,
    /// <summary>
    /// No charge.
    /// </summary>
    Empty,
    /// <summary>
    /// Neither full nor empty.
    /// </summary>
    Neither,
}

