using Content.Shared.Damage;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Vehicles are objects that have the behavior of moving when a player "operates" them.
/// The details of when the vehicle can operate and who the operator is are not defined here.
/// This simply contains the baseline behavior of the vehicle itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The driver of this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    /// <summary>
    /// Simple whitelist for determining who can operate this vehicle.
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

    /// <summary>
    /// Whether the operator requires hands to operate this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresHands = true;

    /// <summary>
    /// Whether the operator can attack while operating this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanAttack;
}

[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    HasOperator,    // The vehicle has a valid operator
    CanRun          // The vehicle can be moved by the operator (turned on :flushed:)
}
