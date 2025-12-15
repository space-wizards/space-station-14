using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Remotes.Components;

/// <summary>
/// Component for door remote devices, that allow you to control doors from a distance.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DoorRemoteComponent : Component
{
    /// <summary>
    /// Currently selected mode. The mode dictates what device would do upon
    /// interaction with door.
    /// </summary>
    [DataField, AutoNetworkedField]
    public OperatingMode Mode = OperatingMode.OpenClose;

    /// <summary>
    /// Modes with metadata that could be displayed in the device mode change menu.
    /// </summary>
    [DataField]
    public List<DoorRemoteModeInfo> Options;

    /// <summary>
    /// Does the remote allow the user to control doors that they have access to, even if the remote itself does not?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IncludeUserAccess;

    /// <summary>
    /// Client-side only field for checking if StatusControl requires update.
    /// </summary>
    /// <remarks>
    /// StatusControl is updated inside loop and cannot understand
    /// when state is of component it looks for is restored, thus mispredicting. To avoid that,
    /// client-side system basically controls behaviour of StatusControl updates using this field.
    /// </remarks>
    public bool IsStatusControlUpdateRequired;
}

/// <summary>
/// Remote door device mode with data that is required for menu display.
/// </summary>
[DataDefinition]
public sealed partial class DoorRemoteModeInfo
{
    /// <summary>
    /// Icon that should represent the option in the radial menu.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Tooltip describing the option in the radial menu.
    /// </summary>
    [DataField(required: true)]
    public LocId Tooltip;

    /// <summary>
    /// Mode option.
    /// </summary>
    [DataField(required: true)]
    public OperatingMode Mode;
}

[Serializable, NetSerializable]
public enum OperatingMode : byte
{
    OpenClose,
    ToggleBolts,
    ToggleEmergencyAccess
}
