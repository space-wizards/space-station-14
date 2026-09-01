namespace Content.Shared.GameTicking.Components;

/// <summary>
///     Added to game rules before <see cref="GameRuleEndedEvent"/>.
///     Mutually exclusive with <seealso cref="ActiveGameRuleComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class EndedGameRuleComponent : Component;
