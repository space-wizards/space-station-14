using Content.Server.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Magic;

public sealed partial class ChainFireballSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChainFireballComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(Entity<ChainFireballComponent> ent, ref ProjectileHitEvent args)
    {
        if (_random.Prob(ent.Comp.DisappearChance))
            return;

        // spawn new fireball on target
        Spawn(args.Target, ent.Comp.IgnoredTargets);

        QueueDel(ent);
    }

    public bool Spawn(EntityUid source, List<EntityUid> ignoredTargets)
    {
        var lookup = _lookup.GetEntitiesInRange(source, 5f);

        List<EntityUid> mobs = new();
        foreach (var look in lookup)
        {
            if (ignoredTargets.Contains(look)
            || !HasComp<StatusEffectsComponent>(look)) // ignore non mobs
                continue;

            mobs.Add(look);
        }
        if (mobs.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail-notarget"), source, source);
            return false;
        }

        return Spawn(source, mobs[_random.Next(0, mobs.Count - 1)], ignoredTargets);
    }
    public bool Spawn(EntityUid source, EntityUid target, List<EntityUid> ignoredTargets)
    {
        return SpawnFireball(source, target, ignoredTargets);
    }
    public bool SpawnFireball(EntityUid uid, EntityUid target, List<EntityUid> ignoredTargets)
    {
        var ball = Spawn("FireballChain", Transform(uid).Coordinates);

        // set ignore list if it wasn't set already
        if (TryComp<ChainFireballComponent>(ball, out var sfc))
            sfc.IgnoredTargets = sfc.IgnoredTargets.Count > 0 ? sfc.IgnoredTargets : ignoredTargets;

        // launch it towards the target
        var fromCoords = _transform.GetMapCoordinates(uid);
        var toCoords = _transform.GetMapCoordinates(target);
        var userVelocity = _physics.GetMapLinearVelocity(uid);

        var direction = toCoords.Position - fromCoords.Position;

        _gun.ShootProjectile(ball, direction, userVelocity, uid, uid);

        return true;
    }
}
