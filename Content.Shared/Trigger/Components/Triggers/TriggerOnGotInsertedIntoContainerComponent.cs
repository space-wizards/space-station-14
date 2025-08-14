using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it gets inserted into a container.
/// The user is the entity being inserted into the container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotInsertedIntoContainerComponent : BaseTriggerOnXComponent;
