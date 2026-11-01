using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Damage.ForceSay;

/// <inheritdoc cref="DamageForceSayComponent"/>
public sealed partial class DamageForceSaySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageForceSayComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<DamageForceSayComponent, MobStateChangedEvent>(OnMobStateChanged);

        // need to raise after mobthreshold
        // so that we don't accidentally raise one for damage before one for mobstate
        // (this won't double raise, because of the cooldown)
        SubscribeLocalEvent<DamageForceSayComponent, DamageDealtEvent>(OnDamageDealt, after: new[] { typeof(MobThresholdSystem) });
        SubscribeLocalEvent<DamageForceSayComponent, SleepStateChangedEvent>(OnSleep);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<AllowNextCritSpeechComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.Timeout)
                continue;

            RemCompDeferred<AllowNextCritSpeechComponent>(uid);
        }
    }

    private void TryForceSay(Entity<DamageForceSayComponent> ent, bool useSuffix = true)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        // disallow if cooldown hasn't ended
        if (ent.Comp.NextAllowedTime != null &&
            _timing.CurTime < ent.Comp.NextAllowedTime)
            return;

        var ev = new BeforeForceSayEvent(ent.Comp.ForceSayStringDataset);
        RaiseLocalEvent(ent, ev);

        if (!ProtoMan.Resolve(ev.Prefix, out var prefixList))
            return;

        var suffix = Loc.GetString(_random.Pick(prefixList.Values));

        // set cooldown & raise event
        ent.Comp.NextAllowedTime = _timing.CurTime + ent.Comp.Cooldown;
        RaiseNetworkEvent(new DamageForceSayEvent { Suffix = useSuffix ? suffix : null }, actor.PlayerSession);
    }

    private void AllowNextSpeech(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var nextCrit = EnsureComp<AllowNextCritSpeechComponent>(uid);

        // timeout is *3 ping to compensate for roundtrip + leeway
        nextCrit.Timeout = _timing.CurTime + TimeSpan.FromMilliseconds(actor.PlayerSession.Ping * 3);
    }

    private void OnSleep(Entity<DamageForceSayComponent> ent, ref SleepStateChangedEvent args)
    {
        if (!args.FellAsleep)
            return;

        if (Comp<MobStateComponent>(ent).CurrentState != MobState.Alive)
            return;

        TryForceSay(ent);
        AllowNextSpeech(ent);
    }

    private void OnStunned(Entity<DamageForceSayComponent> ent, ref StunnedEvent args)
    {
        TryForceSay(ent);
    }

    private void OnDamageDealt(Entity<DamageForceSayComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.AnyPositive || args.Total < ent.Comp.DamageThreshold)
            return;

        if (ent.Comp.ValidDamageGroups != null)
        {
            var totalApplicableDamage = FixedPoint2.Zero;
            foreach (var (group, value) in args.Damage.GetDamagePerGroup(ProtoMan))
            {
                if (!ent.Comp.ValidDamageGroups.Contains(group))
                    continue;

                totalApplicableDamage += value;
            }

            if (totalApplicableDamage < ent.Comp.DamageThreshold)
                return;
        }

        TryForceSay(ent);
    }

    private void OnMobStateChanged(Entity<DamageForceSayComponent> ent, ref MobStateChangedEvent args)
    {
        if (args is not { OldMobState: MobState.Alive, NewMobState: MobState.Critical or MobState.Dead })
            return;

        // no suffix for the drama
        // LING IN MAI-
        TryForceSay(ent, false);
        AllowNextSpeech(ent);
    }
}
