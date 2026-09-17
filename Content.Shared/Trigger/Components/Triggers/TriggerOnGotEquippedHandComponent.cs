using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an item when it is equipped into a hand slot.
/// The user is the entity that picked the item up.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnGotEquippedHandComponent : BaseTriggerOnXComponent;
