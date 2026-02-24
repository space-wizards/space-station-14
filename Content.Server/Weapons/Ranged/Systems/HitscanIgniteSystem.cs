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
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedIgnitionSourceSystem _ignitionSourceSystem = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanIgniteComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }
    //The hitscan has hit the target, rolls a chance to ignite and ignite if it succeeds.
    private void OnHitscanHit(Entity<HitscanIgniteComponent> ent, ref HitscanRaycastFiredEvent args)
    {

        //Rolls a chance for the laser to ignite the target. Cancels if it fails.
        if (!_robustRandom.Prob(ent.Comp.IgniteChance))
            return;

        //If the roll succeeds, the target is set on fire.
        var target = args.Data.HitEntity;
        var stackAmount = ent.Comp.FireStacks;

        if (target == null)
            return;

        _flammable.AdjustFireStacks(target.Value, stackAmount, null, true);

    }
}
