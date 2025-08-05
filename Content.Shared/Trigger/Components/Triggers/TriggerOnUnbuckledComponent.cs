using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the component parent is unbuckled.
/// This is intended to be used on buckle-able entities like mobs.
/// The parent object should be the entity that is being buckled to something else.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUnbuckledComponent : BaseTriggerOnXComponent;
