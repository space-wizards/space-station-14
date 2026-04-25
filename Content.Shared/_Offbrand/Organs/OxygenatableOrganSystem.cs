using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Organs;

public sealed class OxygenatableOrganSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PerfusionSystem _perfusion = default!;
    [Dependency] private readonly DamageableOrganSystem _damageableOrgan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OxygenatableOrganComponent, BodyRelayedEvent<SuicideEvent>>(OnSuicide);
        SubscribeLocalEvent<OxygenatableOrganComponent, BodyRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<OxygenatableDamageableOrganComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OxygenatableDamageableOrganComponent, BodyRelayedEvent<ApplyMetabolicMultiplierEvent>>(OnApplyMetabolicMultiplier);
    }

    private void OnRejuvenate(Entity<OxygenatableOrganComponent> ent, ref BodyRelayedEvent<RejuvenateEvent> args)
    {
        ChangeOxygenation(ent.AsNullable(), ent.Comp.MaxOxygen - ent.Comp.Oxygen);
    }

    private void OnSuicide(Entity<OxygenatableOrganComponent> ent, ref BodyRelayedEvent<SuicideEvent> args)
    {
        ChangeOxygenation(ent.AsNullable(), -ent.Comp.Oxygen);
    }

    /// <summary>
    /// Changes the oxygenation of an organ.
    /// </summary>
    /// <param name="organ">The organ to change the oxygen on.</param>
    /// <param name="amount">The delta to change by.</param>
    /// <seealso cref="OrganOxygenChangedEvent" />
    /// <returns>The actual oxygen delta.</returns>
    public FixedPoint2 ChangeOxygenation(Entity<OxygenatableOrganComponent?> organ, FixedPoint2 amount)
    {
        if (!Resolve(organ, ref organ.Comp))
            return FixedPoint2.Zero;

        organ.Comp.Oxygen = FixedPoint2.Clamp(organ.Comp.Oxygen + amount, FixedPoint2.Zero, organ.Comp.MaxOxygen);
        Dirty(organ);
        var delta = organ.Comp.Oxygen - amount;
        if (delta != FixedPoint2.Zero)
        {
            var evt = new OrganOxygenChangedEvent();
            RaiseLocalEvent(organ, ref evt);
        }

        return delta;
    }

    private void OnApplyMetabolicMultiplier(Entity<OxygenatableDamageableOrganComponent> ent, ref BodyRelayedEvent<ApplyMetabolicMultiplierEvent> args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Args.Multiplier;
        Dirty(ent);
    }

    private void OnMapInit(Entity<OxygenatableDamageableOrganComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<OrganComponent, OxygenatableOrganComponent, OxygenatableDamageableOrganComponent, DamageableOrganComponent>();
        while (enumerator.MoveNext(out var uid, out var organ, out var oxy, out var oxyDamage, out var damage))
        {
            if (oxyDamage.LastUpdate is not { } last || last + oxyDamage.AdjustedUpdateInterval >= _timing.CurTime || damage.Damage >= damage.MaxDamage)
                continue;

            oxyDamage.LastUpdate = _timing.CurTime;
            DoUpdate((uid, organ, oxy, oxyDamage, damage));
            Dirty(uid, oxyDamage);
        }
    }

    private void DoHeal(Entity<OrganComponent, OxygenatableOrganComponent, OxygenatableDamageableOrganComponent, DamageableOrganComponent> ent)
    {
        if (ent.Comp4.Damage == FixedPoint2.Zero)
            return;

        var passivelyHealable = ent.Comp4.Damage < ent.Comp3.MaxPassivelyHealableDamage;
        var stageHealable = ent.Comp4.Damage.Float() % ent.Comp3.DamageStageSize.Float() <= ent.Comp3.DamageStageMaximumHealing.Float();
        var heal = passivelyHealable || stageHealable;

        var evt = new BeforeHealOrganOxygenDamage(heal, ent);
        if (ent.Comp1.Body is { } body)
            RaiseLocalEvent(body, ref evt);

        if (!evt.Heal)
            return;

        _damageableOrgan.ChangeDamage((ent, ent), ent.Comp3.DamageHealing);
    }

    private void DoDamage(Entity<OrganComponent, OxygenatableOrganComponent, OxygenatableDamageableOrganComponent, DamageableOrganComponent> ent, FixedPoint2 oxygenation, System.Random rand)
    {
        var damageThreshold = ent.Comp3.OxygenationDamageThresholds.LowestMatch(oxygenation);

        if (damageThreshold is not (var chance, (var atMost, var amount)) || ent.Comp4.Damage > atMost)
        {
            DoHeal(ent);
            return;
        }

        var evt = new BeforeDealOrganOxygenDamage(chance, ent);
        if (ent.Comp1.Body is { } body)
            RaiseLocalEvent(body, ref evt);

        if (!rand.Prob(evt.Chance))
            return;

        _damageableOrgan.ChangeDamage((ent, ent), amount);
    }

    private void DoOxygen(Entity<OrganComponent, OxygenatableOrganComponent, OxygenatableDamageableOrganComponent, DamageableOrganComponent> ent, FixedPoint2 oxygenation, System.Random rand)
    {
        var depletionThreshold = ent.Comp3.OxygenDepletionThresholds.LowestMatch(oxygenation);

        if (depletionThreshold is not (var chance, var amount))
        {
            if (ent.Comp2.Oxygen < ent.Comp2.MaxOxygen)
                ChangeOxygenation((ent, ent), ent.Comp3.OxygenRegeneration);

            return;
        }

        var evt = new BeforeDepleteOrganOxygen(chance, ent);
        if (ent.Comp1.Body is { } body)
            RaiseLocalEvent(body, ref evt);

        if (!rand.Prob(evt.Chance))
            return;

        ChangeOxygenation((ent, ent), -amount);
    }


    private void DoUpdate(
        Entity<OrganComponent, OxygenatableOrganComponent, OxygenatableDamageableOrganComponent,
            DamageableOrganComponent> ent)
    {
        var oxygenation = ent.Comp1.Body is { } body
            ? TryComp<PerfusionComponent>(body, out var perfusion)
                ? _perfusion.Spo2((body, perfusion))
                : 1
            : FixedPoint2.Zero;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        DoOxygen(ent, oxygenation, rand);
        DoDamage(ent, oxygenation, rand);
    }
}
