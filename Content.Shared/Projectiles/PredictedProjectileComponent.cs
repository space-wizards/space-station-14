using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

/// <summary>
/// Links an authoritative server projectile to the shooter's visual-only predicted projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredictedProjectileComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Shooter;

    [AutoNetworkedField]
    public uint PredictionId;

    [AutoNetworkedField]
    public ushort ProjectileIndex;
}
