using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity with <see cref="Sticky.Components.StickyComponent"/> is stuck to something.
/// The user is the player doing so.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStuckComponent : BaseTriggerOnXComponent;
