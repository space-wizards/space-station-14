using Robust.Shared.GameStates;
using Robust.Shared.Map;

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

    /// <summary>
    /// Authoritative map position where this projectile was fired.
    /// Used to validate the complete path of client-reported collisions.
    /// </summary>
    [DataField]
    public MapCoordinates Origin;

    [DataField]
    public bool Hit;

    [DataField]
    public bool Reconciled;

}
