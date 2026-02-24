using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
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
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanIgniteComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanIgniteComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        //If the target is not flammable, the hit does not ignite.
        if (!TryComp<FlammableComponent>(ent, out var flammable))
            return;

        //Rolls a random chance when the target is hit.
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(args.Data.Gun).Id);
        var rand = new System.Random(seed);

        //If the random roll fails, it doesn't ignite.
        if (!rand.Prob(ent.Comp.IgniteChance))
            return;

        //If the roll succeeds, the target is set on fire.
        var target = args.Data.HitEntity;
        var stackAmount = 1;

        if (target == null)
            return;

        _flammable.AdjustFireStacks(target.Value, stackAmount, null, true);

    }
}
