using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanReflectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanReflectComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanReflectComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (hitscan.Comp.ReflectiveType == ReflectType.None || args.HitEntity == null)
            return;

        if (hitscan.Comp.CurrentReflections >= hitscan.Comp.MaxReflections)
            return;

        var ev = new HitScanReflectAttemptEvent(args.Shooter ?? args.Gun, args.Gun, hitscan.Comp.ReflectiveType, args.ShotDirection, false);
        RaiseLocalEvent(args.HitEntity.Value, ref ev);

        if (!ev.Reflected)
            return;

        hitscan.Comp.CurrentReflections++;

        args.Canceled = true;

        var fromEffect = Transform(args.HitEntity.Value).Coordinates;

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ShotDirection = ev.Direction,
            Gun = args.Gun,
            Shooter = args.HitEntity.Value,
        };

        RaiseLocalEvent(hitscan, ref hitFiredEvent);
    }
}
