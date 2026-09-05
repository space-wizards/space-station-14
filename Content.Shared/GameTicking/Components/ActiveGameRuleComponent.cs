namespace Content.Shared.GameTicking.Components;

/// <summary>
///     Added to game rules before <see cref="GameRuleStartedEvent"/> and removed before <see cref="GameRuleEndedEvent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGameRuleComponent : Component;
