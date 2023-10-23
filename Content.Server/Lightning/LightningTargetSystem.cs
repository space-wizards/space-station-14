using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The component allows lightning to strike this target. And determining the behavior of the target when struck by lightning.
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningTargetComponent, HitByLightningEvent>(OnHitByLightning);
    }

    private void OnHitByLightning(Entity<LightningTargetComponent> uid, ref HitByLightningEvent args)
    {

        if (!uid.Comp.LightningExplode)
            return;

        _explosionSystem.QueueExplosion(
            Transform(uid).MapPosition,
            uid.Comp.ExplosionPrototype,
            uid.Comp.TotalIntensity, uid.Comp.Dropoff,
            uid.Comp.MaxTileIntensity,
            canCreateVacuum: false);
    }
}
