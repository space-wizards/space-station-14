using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The component allows lightning to strike this target. And determining the behavior of the target when struck by lightning.
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningTargetComponent, HitByLightningEvent>(OnHitByLightning);
    }

    private void OnHitByLightning(Entity<LightningTargetComponent> uid, ref HitByLightningEvent args)
    {
        DamageSpecifier damage = new();
        damage.DamageDict.Add("Structural", uid.Comp.DamageFromLightning);
        _damageable.ChangeDamage(uid.Owner, damage, true);

        if (uid.Comp.LightningExplode)
        {
            _explosionSystem.QueueExplosion(
                _transform.GetMapCoordinates(uid),
                uid.Comp.ExplosionPrototype,
                uid.Comp.TotalIntensity, uid.Comp.Dropoff,
                uid.Comp.MaxTileIntensity,
                uid,
                canCreateVacuum: false);
        }
    }
}
