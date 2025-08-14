using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it gets removed from a container.
/// The user is the owner of the container the entity is being removed from.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotRemovedFromContainerComponent : BaseTriggerOnXComponent;
