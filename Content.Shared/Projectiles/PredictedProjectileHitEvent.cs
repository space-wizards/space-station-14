using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles;

/// <summary>
/// Reports a collision made by the shooter's predicted projectile.
/// The server validates every reported target against its own projectile and position history.
/// </summary>
[Serializable, NetSerializable]
public sealed class PredictedProjectileHitEvent(
    uint predictionId,
    ushort projectileIndex,
    HashSet<(NetEntity Entity, MapCoordinates Coordinates)> hits) : EntityEventArgs
{
    public readonly uint PredictionId = predictionId;
    public readonly ushort ProjectileIndex = projectileIndex;
    public readonly HashSet<(NetEntity Entity, MapCoordinates Coordinates)> Hits = hits;
}

[Serializable, NetSerializable]
public sealed class PredictedProjectileReconcileEvent(uint predictionId, ushort projectileIndex) : EntityEventArgs
{
    public readonly uint PredictionId = predictionId;
    public readonly ushort ProjectileIndex = projectileIndex;
}
