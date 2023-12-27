using Content.Server.Lightning;
using Content.Server.Tesla.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// Fires electric arcs at surrounding objects.
/// </summary>
public sealed class LightningArcShooterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightningArcShooterComponent, MapInitEvent>(OnShooterMapInit);
        SubscribeLocalEvent<LightningArcShooterComponent, EntityUnpausedEvent>(OnShooterUnpaused);
    }

    private void OnShooterMapInit(EntityUid uid, LightningArcShooterComponent component, ref MapInitEvent args)
    {
        component.NextShootTime = _gameTiming.CurTime;
    }

    private void OnShooterUnpaused(EntityUid uid, LightningArcShooterComponent component, ref EntityUnpausedEvent args)
    {
        component.NextShootTime += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightningArcShooterComponent>();
        while (query.MoveNext(out var uid, out var arcShooter))
        {
            if (arcShooter.NextShootTime > _gameTiming.CurTime)
                continue;

            ArcShoot(uid, arcShooter);
            var delay = TimeSpan.FromSeconds(_random.NextFloat(arcShooter.ShootMinInterval, arcShooter.ShootMaxInterval));
            arcShooter.NextShootTime += delay;
        }
    }

    private void ArcShoot(EntityUid uid, LightningArcShooterComponent component)
    {
        var arcs = _random.Next(1, component.MaxLightningArc);
        _lightning.ShootRandomLightnings(uid, component.ShootRange, arcs, component.LightningPrototype, component.ArcDepth);
    }
}
