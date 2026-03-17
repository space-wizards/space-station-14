using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when something is unstrapped from the entity.
/// This is intended to be used on objects like chairs or beds.
/// The user is the entity unstrapped from the component owner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUnstrappedComponent : BaseTriggerOnXComponent;
