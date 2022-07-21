namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// Configures a game rule, providing information like what maps to use or how long to run.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class GameRuleConfiguration
{
    /// <summary>
    /// The game rule this configuration is intended for.
    /// </summary>
    public abstract string Id { get; }
}
