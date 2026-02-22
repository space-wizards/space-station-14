using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.IgnitionSource.SharedIgnitionSourceSystem;
using Content.Shared.IgnitionSource;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanIgniteSystem : EntitySystem
{
    [Dependency] private readonly SharedIgnitionSourceSystem _ignitionSourceSystem = default!;
    [Dependency] private readonly SharedFlammableSystem _flammable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanIgniteComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(EntityUid uid, Entity<HitscanIgniteComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(args.Data.Gun).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.IgniteChance))
            return;

        if (!TryComp<FlammableComponent>(uid, out var flammable))
            return;

        _ignitionSourceSystem.SetIgnited((uid, flammable));
        //_flammable.Ignite(args.Data.HitEntity);

    }
}
