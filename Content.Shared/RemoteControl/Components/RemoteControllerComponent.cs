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
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Controlled = null;
}
