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

    [DataField]
    public bool Hit;

    [DataField]
    public bool Reconciled;

    /// <summary>
    /// Targets already processed through a client-reported collision.
    /// Prevents a penetrating projectile from damaging the same target again when it catches up physically.
    /// </summary>
    public readonly HashSet<EntityUid> ProcessedTargets = new();
}
