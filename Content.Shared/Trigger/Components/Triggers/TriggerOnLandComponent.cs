using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the entity exits a floating or thrown state and lands on a surface.
/// The user is the thrower.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnLandComponent : BaseTriggerOnXComponent;
