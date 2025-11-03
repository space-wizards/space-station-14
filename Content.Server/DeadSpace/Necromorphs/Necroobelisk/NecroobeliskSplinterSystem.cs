// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Threading;
using Content.Server.DeadSpace.Necromorphs.Necroobelisk.Components;
using Content.Server.Emp;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.DeadSpace.Necromorphs.Necroobelisk;

public sealed class NecroobeliskSplinterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly EmpSystem _epm = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedSanitySystem _sharedSanity = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
                _charges.AddCharges((uid, charges), 1);
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
            if (_charges.IsEmpty((uid, charges)))
                return;

            _charges.TryUseCharge((uid, charges));
        }

        if (component.SoundHeadaches != null)
        {
            if (TryComp<MindContainerComponent>(args.Target.Value, out var mind)
                && TryComp<MindComponent>(mind.Mind, out var mindComp)
                && _player.TryGetSessionById(mindComp.UserId, out var session))
            {
                var playerFilter = Filter.Empty().AddPlayer(session);
                _audio.PlayGlobal(component.SoundHeadaches, playerFilter, false);
            }
        }

        _stun.TryUpdateParalyzeDuration(args.Target.Value, TimeSpan.FromSeconds(component.Duration));
        _sharedSanity.TryAddSanityLvl(args.Target.Value, -component.SanityDamage);
    }

    private void OnAfterStoper(EntityUid uid,
        NecroobeliskSplinterComponent component,
        NecroSplinterAfterStoperEvent args)
    {
        var entities =
            _lookup.GetEntitiesInRange<SanityComponent>(_transform.GetMapCoordinates(uid, Transform(uid)),
                component.Range);

        if (component.SoundHeadaches != null)
        {
            var playerFilter = Filter.Empty().AddInRange(_transform.GetMapCoordinates(uid), component.Range);
            _audio.PlayGlobal(component.SoundHeadaches, playerFilter, false);
        }

        _epm.EmpPulse(_transform.GetMapCoordinates(uid),
            component.Range * 2,
            component.EnergyConsumption,
            TimeSpan.FromSeconds(component.Duration));

        HashSet<Entity<SanityComponent>> targets = new HashSet<Entity<SanityComponent>>();

        foreach (var entity in entities)
        {
            _stun.TryUpdateParalyzeDuration(entity.Owner, TimeSpan.FromSeconds(component.Duration));
        }

        if (entities.Count > 0)
            CauseDamageSanity(uid, targets, component);
    }

    private void CauseDamageSanity(EntityUid uid,
        HashSet<Entity<SanityComponent>> targets,
        NecroobeliskSplinterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var impulseCount = 0;
        var time = component.SanityDamageRepeatingTime * 1000;

        var sanityDamageTokenSource = new CancellationTokenSource();

        Timer.SpawnRepeating(time,
            () =>
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
            },
            sanityDamageTokenSource.Token);
    }
}

[ByRefEvent]
public readonly record struct NecroSplinterAfterStoperEvent;
