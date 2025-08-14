using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when something is inserted into it.
/// The user is the entity being inserted into the container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnInsertedIntoContainerComponent : BaseTriggerOnXComponent;
