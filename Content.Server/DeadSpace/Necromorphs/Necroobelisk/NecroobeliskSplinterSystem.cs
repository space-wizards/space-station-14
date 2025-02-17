// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Stunnable;
using Content.Server.Emp;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Server.DeadSpace.Necromorphs.Necroobelisk.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;
using System.Threading;
using Content.Shared.Interaction;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Shared.Timing;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.DeadSpace.Necromorphs.Necroobelisk;

public sealed class NecroobeliskSplinterSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EmpSystem _epm = default!;
    [Dependency] private readonly SharedSanitySystem _sharedSanity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecroobeliskSplinterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NecroobeliskSplinterComponent, NecroSplinterAfterStoperEvent>(OnAfterStoper);
        SubscribeLocalEvent<NecroobeliskSplinterComponent, AfterInteractEvent>(OnAfterInteract);
    }
    private void OnMapInit(EntityUid uid, NecroobeliskSplinterComponent component, MapInitEvent args)
    {
        component.AddChargeTime = _timing.CurTime + component.TimeUtilAddCharge;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NecroobeliskSplinterComponent, LimitedChargesComponent>();
        while (query.MoveNext(out var uid, out var component, out var charges))
        {
            if (_timing.CurTime > component.AddChargeTime)
            {
                _charges.AddCharges(uid, 1, charges);
                component.AddChargeTime = _timing.CurTime + component.TimeUtilAddCharge;
            }
        }
    }
    private void OnAfterInteract(EntityUid uid, NecroobeliskSplinterComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (TryComp<LimitedChargesComponent>(uid, out var charges))
        {
            if (_charges.IsEmpty(uid, charges))
                return;

            _charges.UseCharge(uid, charges);
        }

        if (component.SoundHeadaches != null)
        {
            if (TryComp<MindContainerComponent>(args.Target.Value, out var mind)
            && TryComp<MindComponent>(mind.Mind, out var mindComp) && mindComp.Session != null)
            {
                Filter playerFilter = Filter.Empty().AddPlayer(mindComp.Session);
                _audio.PlayGlobal(component.SoundHeadaches, playerFilter, false);
            }
        }

        _stun.TryParalyze(args.Target.Value, TimeSpan.FromSeconds(component.Duration), true);
        _sharedSanity.TryAddSanityLvl(args.Target.Value, -component.SanityDamage);
    }
    private void OnAfterStoper(EntityUid uid, NecroobeliskSplinterComponent component, NecroSplinterAfterStoperEvent args)
    {
        var entities = _lookup.GetEntitiesInRange<SanityComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.Range);

        if (component.SoundHeadaches != null)
        {
            Filter playerFilter = Filter.Empty().AddInRange(_transform.GetMapCoordinates(uid), component.Range);
            _audio.PlayGlobal(component.SoundHeadaches, playerFilter, false);
        }

        _epm.EmpPulse(_transform.GetMapCoordinates(uid), component.Range * 2, component.EnergyConsumption, component.Duration);
        HashSet<Entity<SanityComponent>> targets = new HashSet<Entity<SanityComponent>>();

        foreach (var entity in entities)
        {
            _stun.TryParalyze(entity.Owner, TimeSpan.FromSeconds(component.Duration), true);
        }

        if (entities.Count > 0)
            CauseDamageSanity(uid, targets, component);
    }
    private void CauseDamageSanity(EntityUid uid, HashSet<Entity<SanityComponent>> targets, NecroobeliskSplinterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        int impulseCount = 0;
        int time = component.SanityDamageRepeatingTime * 1000;

        CancellationTokenSource sanityDamageTokenSource = new CancellationTokenSource();

        Timer.SpawnRepeating(time, () =>
        {
            foreach (var (entity, sanity) in targets)
            {
                if (!EntityManager.EntityExists(entity))
                    continue;

                _sharedSanity.TryAddSanityLvl(entity, -component.SanityDamage, sanity);
            }

            impulseCount++;

            if (!EntityManager.EntityExists(uid))
                sanityDamageTokenSource.Cancel();

            if (impulseCount >= component.SanityDamageImpulseCount)
            {
                sanityDamageTokenSource.Cancel();
                QueueDel(uid);
            }

        }, sanityDamageTokenSource.Token);
    }
}

[ByRefEvent]
public readonly record struct NecroSplinterAfterStoperEvent();
