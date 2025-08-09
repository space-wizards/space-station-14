using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanReflectSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanReflectComponent, HitscanHitEntityEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanReflectComponent> hitscan, ref HitscanHitEntityEvent args)
    {
        if (hitscan.Comp.ReflectiveType == ReflectType.None)
            return;

        if (hitscan.Comp.CurrentReflections >= hitscan.Comp.MaxReflections)
            return;

        var ev = new HitScanReflectAttemptEvent(args.Shooter, args.GunUid, hitscan.Comp.ReflectiveType, args.ShotDirection, false);
        RaiseLocalEvent(args.HitEntity, ref ev);

        if (!ev.Reflected)
            return;

        hitscan.Comp.CurrentReflections++;

        args.Canceled = true;

        var fromEffect = Transform(args.HitEntity).Coordinates;

        var evnt = new HitscanFiredEvent
        {
            FromCoordinates = fromEffect,
            ShotDirection = ev.Direction,
            GunUid = args.GunUid,
            Shooter = args.HitEntity,
        };

        RaiseLocalEvent(hitscan, ref evnt);
    }
}
