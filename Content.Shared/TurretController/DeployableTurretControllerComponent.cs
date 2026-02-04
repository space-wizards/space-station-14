using Content.Shared.Access;
using Content.Shared.Turrets;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.TurretController;

/// <summary>
/// Attached to entities that can set data on linked turret-based entities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDeployableTurretControllerSystem))]
public sealed partial class DeployableTurretControllerComponent : Component
{
    /// <summary>
    /// The states of the turrets linked to this entity, indexed by their device address.
    /// This is used to populate the controller UI with the address and state of linked turrets.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, DeployableTurretState> LinkedTurrets = new();

    /// <summary>
    /// The last armament state index applied to any linked turrets.
    /// Values greater than zero have no additional effect if the linked turrets
    /// do not have the <see cref="BatteryWeaponFireModesComponent"/>
    /// </summary>
    /// <remarks>
    /// -1: Inactive, 0: weapon mode A, 1: weapon mode B, etc.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public int ArmamentState = -1;

    /// <summary>
    /// Access level prototypes that are known to the entity.
    /// Determines what access permissions can be adjusted.
    /// It is also used to populate the controller UI.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessLevels = new();

    /// <summary>
    /// Access group prototypes that are known to the entity.
    /// Determines how access permissions are organized on the controller UI.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessGroupPrototype>> AccessGroups = new();

    /// <summary>
    /// Sound to play when denying access to the device.
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}

[Serializable, NetSerializable]
public sealed class DeployableTurretControllerBoundInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, string> TurretStateByAddress;

    public DeployableTurretControllerBoundInterfaceState(Dictionary<string, string> turretStateByAddress)
    {
        TurretStateByAddress = turretStateByAddress;
    }
}

[Serializable, NetSerializable]
public sealed class DeployableTurretArmamentSettingChangedMessage : BoundUserInterfaceMessage
{
    public int ArmamentState;

    public DeployableTurretArmamentSettingChangedMessage(int armamentState)
    {
        ArmamentState = armamentState;
    }
}

[Serializable, NetSerializable]
public sealed class DeployableTurretExemptAccessLevelChangedMessage : BoundUserInterfaceMessage
{
    public HashSet<ProtoId<AccessLevelPrototype>> AccessLevels;
    public bool Enabled;

    public DeployableTurretExemptAccessLevelChangedMessage(HashSet<ProtoId<AccessLevelPrototype>> accessLevels, bool enabled)
    {
        AccessLevels = accessLevels;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public enum TurretControllerVisuals : byte
{
    ControlPanel,
}

[Serializable, NetSerializable]
public enum DeployableTurretControllerUiKey : byte
{
    Key,
}
