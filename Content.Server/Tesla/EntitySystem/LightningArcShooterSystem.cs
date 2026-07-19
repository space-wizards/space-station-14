using Content.Server.Lightning;
using Content.Server.Tesla.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// Fires electric arcs at surrounding objects.
/// </summary>
public sealed partial class LightningArcShooterSystem : EntitySystem
{
    private static readonly EntityTimerId ShootTimer = new("shoot");

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightningArcShooterComponent, MapInitEvent>(OnShooterMapInit);
        SubscribeLocalEvent<LightningArcShooterComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnShooterMapInit(EntityUid uid, LightningArcShooterComponent component, ref MapInitEvent args)
    {
        if (component.Instant)
            component.NextShootTime = _gameTiming.CurTime;
        else
            component.NextShootTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.ShootMaxInterval);
        _timers.SetTimerAt<LightningArcShooterComponent>((uid, component), ShootTimer, component.NextShootTime);
    }

    private void OnTimer(Entity<LightningArcShooterComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != ShootTimer)
            return;

        ArcShoot(ent, ent.Comp);
        var delay = TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.ShootMinInterval, ent.Comp.ShootMaxInterval));
        ent.Comp.NextShootTime = args.ScheduledTime + delay;
        _timers.SetTimerAt(ent, ShootTimer, ent.Comp.NextShootTime);
    }

    private void ArcShoot(EntityUid uid, LightningArcShooterComponent component)
    {
        var arcs = _random.Next(1, component.MaxLightningArc);
        _lightning.ShootRandomLightnings(uid, component.ShootRange, arcs, component.LightningPrototype, component.ArcDepth);
    }
}
