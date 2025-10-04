using Content.Shared.Body.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class BrainDamageSystem : EntitySystem
{
    [Dependency] private readonly HeartSystem _heart = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainDamageComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<BrainDamageComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BrainDamageComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BrainDamageOxygenationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BrainDamageOxygenationComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnSuicide(Entity<BrainDamageComponent> ent, ref SuicideEvent args)
    {
        KillBrain(ent.AsNullable());
        args.Handled = true;
    }

    private void OnRejuvenate(Entity<BrainDamageComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Oxygen = ent.Comp.MaxOxygen;
        ent.Comp.Damage = 0;
        Dirty(ent);

        var notifOxygen = new AfterBrainOxygenChanged();
        RaiseLocalEvent(ent, ref notifOxygen);

        var notifDamage = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notifDamage);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnMapInit(Entity<BrainDamageOxygenationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
    }

    private void OnStartup(Entity<BrainDamageComponent> ent, ref ComponentStartup args)
    {
        var notifOxygen = new AfterBrainOxygenChanged();
        RaiseLocalEvent(ent, ref notifOxygen);

        var notifDamage = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notifDamage);
    }

    private void OnApplyMetabolicMultiplier(Entity<BrainDamageOxygenationComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<BrainDamageComponent, BrainDamageOxygenationComponent, HeartrateComponent>();
        while (enumerator.MoveNext(out var uid, out var brain, out var oxygenation, out var heartrate))
        {
            if (oxygenation.LastUpdate is not { } last || last + oxygenation.AdjustedUpdateInterval >= _timing.CurTime || brain.Damage >= brain.MaxDamage)
                continue;

            oxygenation.LastUpdate = _timing.CurTime;
            DoUpdate((uid, brain, oxygenation, heartrate));
            Dirty(uid, oxygenation);
        }
    }

    public void KillBrain(Entity<BrainDamageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Oxygen = 0;
        ent.Comp.Damage = ent.Comp.MaxDamage;
        Dirty(ent);

        var notifOxygen = new AfterBrainOxygenChanged();
        RaiseLocalEvent(ent, ref notifOxygen);

        var notifDamage = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notifDamage);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    public void TryChangeBrainDamage(Entity<BrainDamageComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Damage = FixedPoint2.Clamp(ent.Comp.Damage + amount, FixedPoint2.Zero, ent.Comp.MaxDamage);
        Dirty(ent);

        var notif = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notif);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }
    public void TryChangeBrainOxygenation(Entity<BrainDamageComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Oxygen = FixedPoint2.Clamp(ent.Comp.Oxygen + amount, FixedPoint2.Zero, ent.Comp.MaxOxygen);
        Dirty(ent);

        var notif = new AfterBrainOxygenChanged();
        RaiseLocalEvent(ent, ref notif);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void DoOxygen(Entity<BrainDamageComponent, BrainDamageOxygenationComponent, HeartrateComponent> ent, FixedPoint2 oxygenation, System.Random rand)
    {
        var depletionThreshold = ent.Comp2.OxygenDepletionThresholds.LowestMatch(oxygenation);

        if (depletionThreshold is not (var chance, var amount))
        {
            if (ent.Comp1.Oxygen < ent.Comp1.MaxOxygen)
            {
                ent.Comp1.Oxygen = FixedPoint2.Min(ent.Comp1.Oxygen + ent.Comp2.OxygenRegeneration, ent.Comp1.MaxOxygen);
                Dirty(ent.Owner, ent.Comp1);

                var increased = new AfterBrainOxygenChanged();
                RaiseLocalEvent(ent, ref increased);
            }
            return;
        }

        var evt = new BeforeDepleteBrainOxygen(chance);
        RaiseLocalEvent(ent, ref evt);

        if (!rand.Prob(evt.Chance))
            return;

        var newValue = FixedPoint2.Max(ent.Comp1.Oxygen - amount, FixedPoint2.Zero);
        if (ent.Comp1.Oxygen == newValue)
            return;

        ent.Comp1.Oxygen = newValue;
        Dirty(ent.Owner, ent.Comp1);

        var notif = new AfterBrainOxygenChanged();
        RaiseLocalEvent(ent, ref notif);
    }

    private void DoHeal(Entity<BrainDamageComponent, BrainDamageOxygenationComponent, HeartrateComponent> ent)
    {
        if (ent.Comp1.Damage == FixedPoint2.Zero)
            return;

        var evt = new BeforeHealBrainDamage(ent.Comp1.Damage < ent.Comp2.MaxPassivelyHealableDamage);
        RaiseLocalEvent(ent, ref evt);

        if (!evt.Heal)
            return;

        var newValue = FixedPoint2.Max(ent.Comp1.Damage - ent.Comp2.DamageHealing, FixedPoint2.Zero);
        if (ent.Comp1.Damage == newValue)
            return;

        ent.Comp1.Damage = newValue;
        Dirty(ent.Owner, ent.Comp1);

        var notif = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notif);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void DoDamage(Entity<BrainDamageComponent, BrainDamageOxygenationComponent, HeartrateComponent> ent, FixedPoint2 oxygenation, System.Random rand)
    {
        var damageThreshold = ent.Comp2.OxygenationDamageThresholds.LowestMatch(oxygenation);

        if (damageThreshold is not (var chance, (var atMost, var amount)) || ent.Comp1.Damage > atMost)
        {
            DoHeal(ent);
            return;
        }

        var evt = new BeforeDealBrainDamage(chance);
        RaiseLocalEvent(ent, ref evt);

        if (!rand.Prob(evt.Chance))
            return;

        var newValue = FixedPoint2.Min(ent.Comp1.Damage + amount, ent.Comp1.MaxDamage);
        if (ent.Comp1.Damage == newValue)
            return;

        ent.Comp1.Damage = newValue;
        Dirty(ent.Owner, ent.Comp1);

        var notif = new AfterBrainDamageChanged();
        RaiseLocalEvent(ent, ref notif);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void DoUpdate(Entity<BrainDamageComponent, BrainDamageOxygenationComponent, HeartrateComponent> ent)
    {
        var oxygenation = _heart.BloodOxygenation((ent.Owner, ent.Comp3));

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        DoOxygen(ent, oxygenation, rand);
        DoDamage(ent, oxygenation, rand);
    }

    public bool IsCritical(Entity<BrainDamageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.Oxygen == 0;
    }
}
