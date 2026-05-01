using Robust.Shared.GameStates;

namespace Content.Shared.RemoteControl.Components;

/// <summary>
/// Indicates this item can be used to start Remote Control.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RCRemoteComponent : Component
{
    /// <summary>
    /// Popup to show when the entity bound to this remote is not accessible.
    /// Happens when the entity is Crit or Dead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RemoteFailPopup = "rc-remote-fail";

    /// <summary>
    /// Popup to show when the remote is used but not bound to any entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RemoteUnboundPopup = "rc-remote-not-synced";

    /// <summary>
    /// Popup to show when the remote is bound to an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RemoteBoundToPopup = "rc-remote-sync";

    /// <summary>
    /// Popup to show when the binding of the remote is wiped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RemoteWipePopup = "rc-remote-wiped";

    /// <summary>
    /// Verb used to wipe the remote.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RemoteWipeVerb = "rc-remote-wipe-verb";

    /// <summary>
    /// The remote control configuration to use during remote control.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RemoteControlConfiguration Config = new();

    /// <summary>
    /// Entity this device will start controlling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BoundTo;
}

