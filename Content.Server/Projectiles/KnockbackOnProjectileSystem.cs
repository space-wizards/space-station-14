using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Server.Projectiles;

public sealed class KnockbackOnProjectileSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnockbackOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid uid, KnockbackOnProjectileHitComponent comp, ref ProjectileHitEvent args)
    {
        var target = args.Target;

        // Resolve transforms for positions
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? projXform) ||
            !EntityManager.TryGetComponent(target, out TransformComponent? targetXform))
            return;

        var projCoords = _transform.GetMapCoordinates(uid);
        var targetCoords = _transform.GetMapCoordinates(target);

        if (projCoords.MapId != targetCoords.MapId)
            return;

        var dir = targetCoords.Position - projCoords.Position;
        if (dir == Vector2.Zero)
            return;

        // Direction vector length = distance to throw in tiles
        var throwVec = dir.Normalized() * comp.Distance;

        _throwing.TryThrow(target, throwVec, comp.Speed, args.Shooter, unanchor: comp.Unanchor);
    }
}
