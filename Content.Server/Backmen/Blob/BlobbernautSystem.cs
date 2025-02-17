using System.Linq;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Backmen.Blob;

public sealed class BlobbernautSystem : SharedBlobbernautSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;

    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobbernautComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlobbernautComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, BlobbernautComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count >= 1)
            return;
        if (!TryComp<BlobTileComponent>(component.Factory, out var blobTileComponent))
            return;
        if (!TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent))
            return;
        if (blobCoreComponent.CurrentChem == BlobChemType.ExplosiveLattice)
        {
            _explosionSystem.QueueExplosion(args.HitEntities.FirstOrDefault(), blobCoreComponent.BlobExplosive, 4, 1, 2, maxTileBreak: 0);
        }
        if (blobCoreComponent.CurrentChem == BlobChemType.ElectromagneticWeb)
        {
            var xform = Transform(args.HitEntities.FirstOrDefault());
            if (_random.Prob(0.2f))
                _empSystem.EmpPulse(_transform.GetMapCoordinates(xform), 3f, 50f, 3f);
        }
    }

    private void OnMobStateChanged(EntityUid uid, BlobbernautComponent component, MobStateChangedEvent args)
    {
        component.IsDead = args.NewMobState switch
        {
            MobState.Dead => true,
            MobState.Alive => false,
            _ => component.IsDead
        };
    }

    protected override DamageSpecifier? TryChangeDamage(string msg, EntityUid ent, DamageSpecifier dmg)
    {
        return _damageableSystem.TryChangeDamage(ent, dmg);
    }
}
