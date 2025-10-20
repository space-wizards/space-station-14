using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Remotes.Components;

/// <summary>
/// Component for door remote device, that helps with manipulating doors on distance.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DoorRemoteComponent : Component
{
    /// <summary>
    /// Currently selected mode. Mode dictates what device would do upon
    /// interaction with door.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public OperatingMode Mode = OperatingMode.OpenClose;

    /// <summary>
    /// Modes with metadata that could be displayed in device mode change menu.
    /// </summary>
    [DataField]
    public List<DoorRemoteModeInfo> Options;

    /// <summary>
    /// Does the remote allow the user to manipulate doors that they have access to, even if the remote itself does not?
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool IncludeUserAccess = false;
}

/// <summary>
/// Remote door device mode with data that is required for menu display.
/// </summary>
[DataDefinition]
public sealed partial class DoorRemoteModeInfo
{
    /// <summary>
    /// Icon that should represent mode as an option in menu.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Tooltip describing option in menu.
    /// </summary>
    [DataField(required: true)]
    public LocId Tooltip;

    /// <summary>
    /// Mode option.
    /// </summary>
    [DataField(required: true)]
    public OperatingMode Mode;
}

public enum OperatingMode : byte
{
    OpenClose,
    ToggleBolts,
    ToggleEmergencyAccess
}
