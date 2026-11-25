using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Component put on the hitscan entity.
/// Triggers when a hitscan raycast is fired.
/// The user is the player shooting it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnHitscanFiredComponent : BaseTriggerOnXComponent;
