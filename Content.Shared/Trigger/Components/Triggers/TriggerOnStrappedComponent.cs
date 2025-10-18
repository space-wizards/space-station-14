using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when something is strapped to the entity.
/// This is intended to be used on objects like chairs or beds.
/// The user is the entity strapped to the component owner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStrappedComponent : BaseTriggerOnXComponent;
