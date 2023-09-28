using Content.Shared.Damage;
using Content.Shared.Damage.ForceSay;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Damage.ForceSay;

/// <inheritdoc cref="DamageForceSayComponent"/>
public sealed class DamageForceSaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    ///     Used so we don't double-send in one tick
    ///     for instance when a damageable event triggers a mobstate change.
    ///
    ///     The 'bool' here is the `UseSuffix` parameter.
    /// </summary>
    private readonly Dictionary<ICommonSession, bool> _toSend = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageForceSayComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<DamageForceSayComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DamageForceSayComponent, DamageChangedEvent>(OnDamageChanged, after: new []{ typeof(MobThresholdSystem)} );
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (session, suffix) in _toSend)
        {
            RaiseNetworkEvent(new DamageForceSayEvent { UseSuffix = suffix }, session);
        }

        _toSend.Clear();

        var query = AllEntityQuery<AllowNextCritSpeechComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.Timeout)
                continue;

            RemCompDeferred<AllowNextCritSpeechComponent>(uid);
        }
    }

    private void TryForceSay(EntityUid uid, bool suffix=true)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _toSend.TryAdd(actor.PlayerSession, suffix);
    }

    private void OnStunned(EntityUid uid, DamageForceSayComponent component, ref StunnedEvent args)
    {
        TryForceSay(uid);
    }

    private void OnDamageChanged(EntityUid uid, DamageForceSayComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased || args.DamageDelta.Total < component.DamageThreshold)
            return;

        if (component.ValidDamageGroups != null)
        {
            FixedPoint2 totalApplicableDamage = FixedPoint2.Zero;
            foreach (var (group, value) in args.DamageDelta.GetDamagePerGroup(_prototype))
            {
                if (!component.ValidDamageGroups.Contains(group))
                    continue;

                totalApplicableDamage += value;
            }

            if (totalApplicableDamage < component.DamageThreshold)
                return;
        }

        TryForceSay(uid);
    }

    private void OnMobStateChanged(EntityUid uid, DamageForceSayComponent component, MobStateChangedEvent args)
    {
        if (args is not { OldMobState: MobState.Alive, NewMobState: MobState.Critical or MobState.Dead })
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        // no suffix for the drama
        // LING IN MAI-
        TryForceSay(uid, false);
        var nextCrit = EnsureComp<AllowNextCritSpeechComponent>(uid);

        // timeout is *3 ping to compensate for roundtrip + leeway
        nextCrit.Timeout = _timing.CurTime + TimeSpan.FromMilliseconds(actor.PlayerSession.Ping * 3);
    }
}
