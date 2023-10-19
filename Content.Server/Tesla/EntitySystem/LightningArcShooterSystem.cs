using Content.Server.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Server.Tesla.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Server.Lightning;
using Content.Shared.Mobs.Components;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// 
/// </summary>
public sealed class LightningArcShooterSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<TeslaEnergyBallComponent, StartCollideEvent>(HandleParticleCollide);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightningArcShooterComponent>();
        while (query.MoveNext(out var uid, out var arcShooter))
        {
            if (arcShooter.NextShootTime > _gameTiming.CurTime)
                return;

            ArcShoot(uid, arcShooter);

            var delay = TimeSpan.FromSeconds(_random.NextFloat(arcShooter.ShootMinInterval, arcShooter.ShootMaxInterval));
            arcShooter.NextShootTime = _gameTiming.CurTime + delay;
            Log.Debug("Некст пиу через " + delay.ToString());
        }
    }

    private void ArcShoot(EntityUid uid, LightningArcShooterComponent component)
    {
        var arcs = _random.Next(component.MaxLightningArc);
        _lightning.ShootRandomLightnings(uid, 20, arcs);
        //var range = 20;
        //var xform = Transform(uid);
        //var targets = _lookup.GetComponentsInRange<LightningPriorityTargetComponent>(xform.MapPosition, range);
        
        //for (var i = 0; i < arcs; i++)
        //{
        //    var target = _random.Pick(targets);
        //    _lightning.ShootLightning(uid, target.Owner);
        //    targets.Remove(target);
        //}
    }
}
