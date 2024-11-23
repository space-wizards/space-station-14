using Robust.Shared.GameStates;

namespace Content.Shared.RemoteControl.Components;
/// <summary>
/// Indicates this entity is currently being remotely controlled by another entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemotelyControllableComponent : Component
{
    /// <summary>
    /// The ability used to return to the original body.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ReturnAbility = null;

    /// <summary>
    /// The examine message shown when examining the entity with this component.
    /// Null means there is no message.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? ExamineMessage;

    /// <summary>
    /// Whether this entity is currently being controlled.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsControlled = false;

    /// <summary>
    /// Which RCRemote this is currently bound to.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? BoundRemote = null;

    /// <summary>
    /// Which entity is currently remotely controlling this entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Controller = null;
}

