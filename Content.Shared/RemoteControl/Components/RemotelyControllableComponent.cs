using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.RemoteControl.Components;

/// <summary>
/// Indicates this entity is able to be remotely controlled by another entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemotelyControllableComponent : Component
{
    /// <summary>
    /// The examine message shown when examining the entity with this component.
    /// Null means there is no message.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? ExamineMessage = "rc-controlled-examine";

    /// <summary>
    /// Action granted to the remotely controlled entity.
    /// Used to return back to body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ReturnActionPrototype = "ActionRCBackToBody";

    /// <summary>
    /// The ability used to return to the original body.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ReturnActionEntity;

    /// <summary>
    /// Whether this entity is currently being controlled.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsControlled;

    /// <summary>
    /// Which RCRemote this is currently bound to.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? BoundRemote;

    /// <summary>
    /// Which entity is currently remotely controlling this entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Controller;
}
