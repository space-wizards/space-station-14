using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers on use in hand.
/// The user the the player holding the item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TriggerOnUseComponent : BaseTriggerOnXComponent;
