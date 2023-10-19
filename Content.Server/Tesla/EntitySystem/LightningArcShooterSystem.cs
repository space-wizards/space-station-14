using Content.Server.Tesla.Components;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Server.Lightning;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// Fires electric arcs at surrounding objects. Has a priority list of what to shoot at.
/// </summary>
public sealed class LightningArcShooterSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    public override void Initialize()
    {
        base.Initialize();

    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightningArcShooterComponent>();
        while (query.MoveNext(out var uid, out var arcShooter))
        {
            if (arcShooter.NextShootTime > _gameTiming.CurTime)
                return;

            Log.Debug("Ща бахнем");
            ArcShoot(uid, arcShooter);
            Log.Debug("Бахнули!");
            var delay = TimeSpan.FromSeconds(_random.NextFloat(arcShooter.ShootMinInterval, arcShooter.ShootMaxInterval));
            arcShooter.NextShootTime = _gameTiming.CurTime + delay;
            Log.Debug("закончили бахать");
        }
    }

    private void ArcShoot(EntityUid uid, LightningArcShooterComponent component)
    {
        var arcs = _random.Next(component.MaxLightningArc);
        _lightning.ShootRandomLightnings(uid, component.ShootRange, arcs, component.LightningPrototype, component.ArcDepth);
    }
}
