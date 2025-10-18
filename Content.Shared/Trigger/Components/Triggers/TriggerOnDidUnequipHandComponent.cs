using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it is drops an item from one of its hand slots.
/// The user is the entity that was dropped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDidUnequipHandComponent : BaseTriggerOnXComponent;
