using Robust.Shared.GameStates;

namespace Content.Shared.RemoteControl.Components;

/// <summary>
/// Indicates this entity is currently remotely controlling another entity.
/// Automatically added to entities that are using remote control.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllerComponent : Component
{
    /// <summary>
    /// Which entity is currently being remotely controlled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Controlled;

    /// <summary>
    /// The remote control configuration to use during remote control.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RemoteControlConfiguration? Config;
}
