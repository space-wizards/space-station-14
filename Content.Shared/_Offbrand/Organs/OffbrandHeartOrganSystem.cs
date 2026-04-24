using Content.Shared._Offbrand.StatusEffects;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Medical;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Organs;

public sealed class OffbrandHeartOrganSystem : EntitySystem
{
    [Dependency] private readonly DamageableOrganSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OffbrandHeartOrganComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, BodyRelayedEvent<HeartBeatEvent>>(OnHeartBeat);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, BodyRelayedEvent<BaseCardiacOutputEvent>>(OnBaseCardiacOutput);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, BodyRelayedEvent<CardiacCompensationEvent>>(OnCardiacCompensation);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, BodyRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<OffbrandHeartOrganComponent, OrganDamageChangedEvent>(OnOrganDamageChanged);
        SubscribeLocalEvent<HeartStopOnHighStrainComponent, PotentialHeartStopEvent>(OnHeartBeatStrain);
        SubscribeLocalEvent<HeartDefibrillatableComponent, BodyRelayedEvent<TargetDefibrillatedEvent>>(OnTargetDefibrillated);
    }

    private void OnOrganGotInserted(Entity<OffbrandHeartOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (ent.Comp.Beating)
        {
            var evt = new HeartStartedEvent();
            RaiseLocalEvent(args.Target, ref evt);
        }
        else
        {
            var stoppedEvt = new HeartStoppedEvent();
            RaiseLocalEvent(args.Target, ref stoppedEvt);
        }
    }

    private void OnOrganGotRemoved(Entity<OffbrandHeartOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        var stoppedEvt = new HeartStoppedEvent();
        RaiseLocalEvent(args.Target, ref stoppedEvt);
    }

    private void OnOrganDamageChanged(Entity<OffbrandHeartOrganComponent> ent, ref OrganDamageChangedEvent args)
    {
        if (!ent.Comp.Beating)
            return;

        if (args.Organ.Comp.Damage >= args.Organ.Comp.MaxDamage)
            StopHeart(ent);
    }

    private void OnRejuvenate(Entity<OffbrandHeartOrganComponent> ent, ref BodyRelayedEvent<RejuvenateEvent> args)
    {
        if (ent.Comp.Beating)
            return;

        StartHeart(ent);
    }

    private float Strain(Entity<OffbrandHeartOrganComponent> ent)
    {
        return Math.Max(ent.Comp.CompensationStrainCoefficient * ent.Comp.Compensation + ent.Comp.CompensationStrainConstant, 0f);
    }

    private void OnHeartBeat(Entity<OffbrandHeartOrganComponent> ent, ref BodyRelayedEvent<HeartBeatEvent> args)
    {
        var stop = new PotentialHeartStopEvent(args.Body, false);
        RaiseLocalEvent(ent, ref stop);

        if (stop.Stop)
        {
            StopHeart(ent);
            return;
        }

        var threshold = ent.Comp.StrainDamageThresholds.HighestMatch(Strain(ent));
        if (threshold is (var chance, var amount))
        {
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
            var rand = new System.Random(seed);

            if (rand.Prob(chance))
                _damageable.ChangeDamage(ent.Owner, amount);
        }
    }

    private void OnBaseCardiacOutput(Entity<OffbrandHeartOrganComponent> ent, ref BodyRelayedEvent<BaseCardiacOutputEvent> args)
    {
        var damage = Comp<DamageableOrganComponent>(ent);

        args.Args = args.Args with
        {
            Output = !ent.Comp.Beating ? 0f : 1f - (damage.Damage.Float() / damage.MaxDamage.Float()),
        };
    }

    private void OnCardiacCompensation(Entity<OffbrandHeartOrganComponent> ent,
        ref BodyRelayedEvent<CardiacCompensationEvent> args)
    {
        var damage = Comp<DamageableOrganComponent>(ent);
        var invert = MathF.Log(args.Args.Demand / args.Args.Supply);
        if (!float.IsFinite(invert))
            throw new InvalidOperationException($"demand/supply {args.Args.Demand}/{args.Args.Supply} is not finite: {invert}");

        var targetCompensation = ent.Comp.CompensationCoefficient * invert + ent.Comp.CompensationConstant;
        var healthFactor = !ent.Comp.Running ? 0f : 1f - (damage.Damage.Float() / damage.MaxDamage.Float());

        ent.Comp.Compensation = Math.Max(targetCompensation * healthFactor, 1f);
        args.Args = args.Args with { Compensation = ent.Comp.Compensation, Strain = Strain(ent) };

        Dirty(ent);
    }

    private void OnHeartBeatStrain(Entity<HeartStopOnHighStrainComponent> ent, ref PotentialHeartStopEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(args.Body))
            return;

        var strain = Strain((ent, Comp<OffbrandHeartOrganComponent>(ent)));
        args.Stop = args.Stop || rand.Prob(ent.Comp.Chance) && strain > ent.Comp.Threshold;
    }

    private void StopHeart(Entity<OffbrandHeartOrganComponent> ent)
    {
        ent.Comp.Beating = false;
        Dirty(ent);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        var stoppedEvt = new HeartStoppedEvent();
        RaiseLocalEvent(body, ref stoppedEvt);
    }

    private void TryRestartHeart(Entity<OffbrandHeartOrganComponent?, DamageableOrganComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        if (ent.Comp2.MaxDamage <= ent.Comp2.Damage || ent.Comp1.Beating)
            return;

        StartHeart((ent, ent.Comp1));
    }

    private void StartHeart(Entity<OffbrandHeartOrganComponent> ent)
    {
        ent.Comp.Beating = true;
        Dirty(ent);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        var evt = new HeartStartedEvent();
        RaiseLocalEvent(body, ref evt);
    }

    private void OnTargetDefibrillated(Entity<HeartDefibrillatableComponent> ent, ref BodyRelayedEvent<TargetDefibrillatedEvent> args)
    {
        TryRestartHeart(ent.Owner);
    }
}
