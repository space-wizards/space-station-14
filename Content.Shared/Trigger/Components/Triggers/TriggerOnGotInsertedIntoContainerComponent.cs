using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it gets inserted into a container.
/// The user is the owner of the container the entity is being inserted into.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotInsertedIntoContainerComponent : BaseTriggerOnXComponent;
