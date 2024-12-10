using Robust.Shared.GameStates;

namespace Content.Shared.RemoteControl.Components;
/// <summary>
/// Indicates this item can be used to start Remote Control.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RCRemoteComponent : Component
{
    /// <summary>
    /// Entity this device will start controlling.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? BoundTo = null;
}

