using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Bed;

public sealed class BedSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;

    private EntityQuery<SleepingComponent> _sleepingQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealOnBuckleComponent, MapInitEvent>(OnHealMapInit);
        SubscribeLocalEvent<HealOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<HealOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<StasisBedComponent, StrappedEvent>(OnStasisStrapped);
        SubscribeLocalEvent<StasisBedComponent, UnstrappedEvent>(OnStasisUnstrapped);
        SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnStasisEmagged);
        SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StasisBedBuckledComponent, GetMetabolicMultiplierEvent>(OnStasisGetMetabolicMultiplier);

        _sleepingQuery = GetEntityQuery<SleepingComponent>();
    }

    private void OnHealMapInit(Entity<HealOnBuckleComponent> ent, ref MapInitEvent args)
    {
        _actConts.EnsureAction(ent.Owner, ref ent.Comp.SleepAction, SleepingSystem.SleepActionId);
        Dirty(ent);
    }

    private void OnStrapped(Entity<HealOnBuckleComponent> bed, ref StrappedEvent args)
    {
        EnsureComp<HealOnBuckleHealingComponent>(bed);
        bed.Comp.NextHealTime = _timing.CurTime + TimeSpan.FromSeconds(bed.Comp.HealTime);
        _actionsSystem.AddAction(args.Buckle, ref bed.Comp.SleepAction, SleepingSystem.SleepActionId, bed);
        Dirty(bed);

        // Single action entity, cannot strap multiple entities to the same bed.
        DebugTools.AssertEqual(args.Strap.Comp.BuckledEntities.Count, 1);
    }

    private void OnUnstrapped(Entity<HealOnBuckleComponent> bed, ref UnstrappedEvent args)
    {
        // If the entity being unbuckled is terminating, we shouldn't try to act upon it, as some components may be gone
        if (!Terminating(args.Buckle.Owner))
        {
            _actionsSystem.RemoveAction(args.Buckle.Owner, bed.Comp.SleepAction);
            _sleepingSystem.TryWaking(args.Buckle.Owner);
        }

        RemComp<HealOnBuckleHealingComponent>(bed);
    }

    private void OnStasisStrapped(Entity<StasisBedComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<StasisBedBuckledComponent>(args.Buckle);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisUnstrapped(Entity<StasisBedComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<StasisBedBuckledComponent>(ent);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisEmagged(Entity<StasisBedComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.Multiplier = 1f / ent.Comp.Multiplier;
        UpdateMetabolisms(ent.Owner);
        Dirty(ent);

        args.Handled = true;
    }

    private void OnPowerChanged(Entity<StasisBedComponent> ent, ref PowerChangedEvent args)
    {
        UpdateMetabolisms(ent.Owner);
    }

    private void OnStasisGetMetabolicMultiplier(Entity<StasisBedBuckledComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo is not { } buckledTo)
            return;

        if (!TryComp<StasisBedComponent>(buckledTo, out var stasis))
            return;

        if (!_powerReceiver.IsPowered(buckledTo))
            return;

        args.Multiplier *= stasis.Multiplier;
    }

    private void UpdateMetabolisms(Entity<StrapComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var buckledEntity in ent.Comp.BuckledEntities)
        {
            _metabolizer.UpdateMetabolicMultiplier(buckledEntity);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HealOnBuckleHealingComponent, HealOnBuckleComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out _, out var bedComponent, out var strapComponent))
        {
            if (_timing.CurTime < bedComponent.NextHealTime)
                continue;

            bedComponent.NextHealTime += TimeSpan.FromSeconds(bedComponent.HealTime);

            Dirty(uid, bedComponent);

            if (strapComponent.BuckledEntities.Count == 0)
                continue;

            foreach (var healedEntity in strapComponent.BuckledEntities)
            {
                if (_mobStateSystem.IsDead(healedEntity))
                    continue;

                var damage = bedComponent.Damage;

                if (_sleepingQuery.HasComp(healedEntity))
                    damage *= bedComponent.SleepMultiplier;

                _damageableSystem.TryChangeDamage(healedEntity, damage, true, origin: uid);
            }
        }
    }
}
