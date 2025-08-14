using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when something is removed from it.
/// The user is the entity being removed from the container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnRemovedFromContainerComponent : BaseTriggerOnXComponent;
