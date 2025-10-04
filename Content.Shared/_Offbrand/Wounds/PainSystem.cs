using Content.Shared.Body.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class PainSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PainComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<PainComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnApplyMetabolicMultiplier(Entity<PainComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PainComponent>();
        while (query.MoveNext(out var uid, out var pain))
        {
            if (pain.LastUpdate is not { } last || last + pain.AdjustedUpdateInterval >= _timing.CurTime)
                continue;

            var delta = _timing.CurTime - last;
            pain.LastUpdate = _timing.CurTime;

            var currentPain = GetPain(uid) * pain.PainMultiplier;

            if (pain.Shock < currentPain)
            {
                var difference = FixedPoint2.Abs(pain.Shock - currentPain);
                var maxIncrease = delta.TotalSeconds * pain.MaxShockIncreasePerSecond;
                pain.Shock += FixedPoint2.Min(difference, maxIncrease);
            }
            else if (pain.Shock > currentPain)
            {
                var difference = FixedPoint2.Abs(pain.Shock - currentPain);
                var maxDecrease =
                    currentPain < pain.DoubleShockRecoveryThreshold * pain.Shock ?
                        delta.TotalSeconds * pain.MaxShockDecreasePerSecond * 2 :
                        delta.TotalSeconds * pain.MaxShockDecreasePerSecond;

                pain.Shock -= FixedPoint2.Min(difference, maxDecrease);
            }
            else
            {
                Dirty(uid, pain);
                continue;
            }

            if (pain.Shock < 0)
                pain.Shock = 0;

            var evt = new AfterShockChangeEvent();
            RaiseLocalEvent(uid, ref evt);

            var overlays = new bPotentiallyUpdateDamageOverlayEventb(uid);
            RaiseLocalEvent(uid, ref overlays, true);

            Dirty(uid, pain);
        }
    }

    private void OnRejuvenate(Entity<PainComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Shock = 0;
        Dirty(ent);

        var evt = new AfterShockChangeEvent();
        RaiseLocalEvent(ent, ref evt);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private FixedPoint2 GetPain(EntityUid ent)
    {
        var evt = new GetPainEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref evt);

        return evt.Pain;
    }

    private void OnMapInit(Entity<PainComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
    }

    public FixedPoint2 GetShock(Entity<PainComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return FixedPoint2.Zero;

        if (ent.Comp.Suppressed)
            return FixedPoint2.Zero;

        return ent.Comp.Shock;
    }

    public void UpdateSuppression(Entity<PainComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var evt = new PainSuppressionEvent(false);
        RaiseLocalEvent(ent, ref evt);

        if (ent.Comp.Suppressed == evt.Suppressed)
            return;

        ent.Comp.Suppressed = evt.Suppressed;

        var notif = new AfterShockChangeEvent();
        RaiseLocalEvent(ent, ref notif);
    }
}
