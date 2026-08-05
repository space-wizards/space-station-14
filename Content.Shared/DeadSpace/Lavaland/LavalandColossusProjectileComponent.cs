namespace Content.Shared.DeadSpace.Lavaland;

/// <summary>
/// Marks a Colossus death bolt so it can hit prone and critical-state targets.
/// </summary>
[RegisterComponent]
public sealed partial class LavalandColossusProjectileComponent : Component
{
    [ViewVariables]
    public EntityUid Boss;
}
