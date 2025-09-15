using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when something is inserted into it.
/// The user is the entity being inserted into the container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnInsertedIntoContainerComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The container to the entity has to be inserted into.
    /// Null will allow all containers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? ContainerId;
}
