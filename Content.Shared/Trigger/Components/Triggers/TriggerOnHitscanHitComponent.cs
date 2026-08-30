using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Component put on the hitscan entity.
/// Triggers when a hitscan raycast is fired, but only if an entity got hit.
/// The user is the entity that got hit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnHitscanHitComponent : BaseTriggerOnXComponent;
