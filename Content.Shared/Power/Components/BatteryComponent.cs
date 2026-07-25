using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell.Components;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Components;

/// <summary>
/// Used for any sort of battery that stores electical power.
/// Can be used as a battery node on the pow3r network. Needs other components to connect to actual networks, see PowerNetworkBatteryComponent.
/// Also used for power cells using <see cref="PowerCellComponent"/> or battery powered guns with intrinsic battery.
/// </summary>
/// <remarks>
/// IMPORTANT: If your battery has an update loop setting the charge every single tick you should set <see cref="Component.NetSyncEnabled"> to false
/// in your prototype to prevent it from getting networked every single tick. However, this will disable prediction.
/// This is mostly needed for anything connected to the power network (APCs, SMES, turrets with battery), as their power supply ramps up over time.
/// Everything else that only has a constant charge rate (e.g. charging/discharging a battery at a certain wattage) or instantaneous power draw (e.g. shooting a gun) is fine being networked.
/// However, you should write your systems to avoid using update loops and instead change the battery's charge rate using <see cref="SharedBatterySystem.RefreshChargeRate"/> and
/// the current charge will automatically be inferred if you use <see cref="SharedBatterySystem.GetCharge"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedBatterySystem))]
public sealed partial class BatteryComponent : Component
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
    /// Used to track state changes for raising <see cref="BatteryStateChangedEvent"/>.
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

