namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for gamemodes that automatically respawn players when they're no longer alive.
/// </summary>
[RegisterComponent, Access(typeof(RespawnRuleSystem))]
public sealed partial class RespawnDeadRuleComponent : Component
{
}
