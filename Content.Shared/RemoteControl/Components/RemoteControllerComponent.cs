using Robust.Shared.GameStates;

namespace Content.Shared.RemoteControl.Components;
/// <summary>
/// Indicates this entity is currently remotely controlling another entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllerComponent : Component
{
    /// <summary>
    /// Which entity is currently being remotely controlled.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Controlled = null;

    /// <summary>
    /// The remote used to control another entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? UsedRemote = null;
}

