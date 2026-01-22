using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it is dropped from a users hands, or directly removed from a users inventory, but not when moved between hands & inventory.
/// The user is the player that was holding or wearing the item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDroppedComponent : BaseTriggerOnXComponent;
