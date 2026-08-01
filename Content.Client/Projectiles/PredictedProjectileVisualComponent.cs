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
    public EntityCoordinates? CoordinatesBeforePredictionReplay;
}

[RegisterComponent]
public sealed partial class HiddenPredictedProjectileComponent : Component
{
    public bool SpriteVisible;
    public bool LightEnabled;
}
