using Content.Shared.Actions;

namespace Content.Shared.Explosion.Components;

/// <summary>
/// Makes the entity trigger when an action is used.
/// The action must raise <see cref="TriggerActionEvent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnActionComponent : Component;

/// <summary>
/// Makes the entity trigger.
/// </summary>
public sealed partial class TriggerActionEvent : InstantActionEvent;
