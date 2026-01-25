using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it gets inserted into a container.
/// The user is the owner of the container the entity is being inserted into.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotInsertedIntoContainerComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The container to the entity has to be inserted into.
    /// Null will allow all containers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? ContainerId;
}
