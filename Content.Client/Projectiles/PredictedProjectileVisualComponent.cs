using Robust.Shared.Map;

namespace Content.Client.Projectiles;

[RegisterComponent]
public sealed partial class PredictedProjectileVisualComponent : Component
{
    public uint PredictionId;
    public ushort ProjectileIndex;
    public MapCoordinates Origin;
    public TimeSpan CreatedAt;
    public TimeSpan? HitAt;
    public float? HitDistance;
    public EntityUid? AuthoritativeProjectile;
    public EntityUid? PendingCollision;
    public MapCoordinates PendingProjectileCoordinates;
    public MapCoordinates PendingContactCoordinates;
    public EntityCoordinates? CoordinatesBeforePredictionReplay;
    public MapCoordinates? CoordinatesBeforePhysics;
    public readonly Dictionary<EntityUid, MapCoordinates> TargetCoordinatesBeforePhysics = new();
}

[RegisterComponent]
public sealed partial class HiddenPredictedProjectileComponent : Component
{
    public bool SpriteVisible;
    public bool LightEnabled;
}
