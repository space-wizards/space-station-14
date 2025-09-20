using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when it is equips an item into one of its hand slots.
/// The user is the entity that was equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDidEquipHandComponent : BaseTriggerOnXComponent;
