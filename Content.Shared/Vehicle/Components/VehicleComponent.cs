using Content.Shared.Damage;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Vehicles are objects that have the behavior of moving when a player "operates" them.
/// The details of when the vehicle can operate and who the operator is are not defined here.
/// This simply contains the baseline behavior of the vehicle itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The driver of this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    /// <summary>
    /// Simple whitelist for determining who can operator this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? OperatorWhitelist;

    /// <summary>
    /// If true, damage to the vehicle will be transferred to the operator.
    /// This damage is modified by <see cref="TransferDamageModifier"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TransferDamage = true;

    /// <summary>
    /// A damage modifier set that adjusts the damage passed from the vehicle to the operator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageModifierSet? TransferDamageModifier;
}

[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    HasOperator,    // The vehicle has a valid operator
    CanRun,         // The vehicle can be moved by the operator (turned on :flushed:)
}

/// <summary>
/// Event raised on operator when they begin to operate a vehicle
/// Values are configured before this event is raised.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct OnVehicleEnteredEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on operator when they stop operating a vehicle.
/// Values are configured after this event is raised.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct OnVehicleExitedEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on vehicle after an operator is set.
/// New operator can be null.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct VehicleOperatorSetEvent(EntityUid? NewOperator, EntityUid? OldOperator);

/// <summary>
/// Event raised on a vehicle to check if it can run/move around.
/// </summary>
[ByRefEvent, UsedImplicitly]
public record struct VehicleCanRunEvent(Entity<VehicleComponent> Vehicle, bool CanRun = true);
