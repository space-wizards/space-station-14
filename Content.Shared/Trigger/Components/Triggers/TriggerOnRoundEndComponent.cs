using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers the entity when the round ends, i.e. the scoreboard appears and post-round begins.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnRoundEndComponent : BaseTriggerOnXComponent;
