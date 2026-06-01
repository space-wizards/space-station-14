using System.Linq;
using Content.Shared._Offbrand.Organs;
using Content.Shared.Body.Systems;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class WoundableBodySystem : OffbrandDamageSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private WoundableOrganSystem _woundableOrgan = default!;
    [Dependency] private WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableBodyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WoundableBodyComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<WoundableBodyComponent, RefreshWoundsEvent>(OnRefreshWounds);
        SubscribeLocalEvent<WoundableBodyComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined, before: [typeof(SharedBloodstreamSystem)]);
    }

    private void OnShutdown(Entity<WoundableBodyComponent> ent, ref ComponentShutdown args)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundComponent>(ent, out var wounds))
            return;

        foreach (var wound in wounds)
        {
            QueueDel(wound);
        }
    }

    private static readonly LocId WoundCountModifier = "wound-count-modifier";

    private void OnHealthBeingExamined(Entity<WoundableBodyComponent> ent, ref HealthBeingExaminedEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        foreach (var organ in body.Organs?.ContainedEntities ?? [])
        {
            if (!_statusEffects.TryEffectsWithComp<WoundDescriptionComponent>(organ, out var wounds))
                continue;

            if (!args.Message.IsEmpty)
            {
                args.Message.PushNewline();
            }

            var counts = new Dictionary<(LocId, LocId?, LocId?), int>();

            foreach (var describable in wounds)
            {
                var wound = Comp<WoundComponent>(describable);
                var damage = wound.Damage.GetTotal();

                if (describable.Comp1.Descriptions.HighestMatch(damage) is not { } message)
                    continue;

                var text = message;
                LocId? bleedingMessage = null;
                LocId? tendedMessage = null;

                if (TryComp<BleedingWoundComponent>(describable, out var bleeding) && _woundable.BleedLevel((describable.Owner, bleeding)) > 0f)
                    bleedingMessage = describable.Comp1.BleedingModifier;

                if (TryComp<TendableWoundComponent>(describable, out var tendable) && tendable.Tended)
                    tendedMessage = describable.Comp1.TendedModifier;

                var triple = (text, bleedingMessage, tendedMessage);

                if (counts.TryGetValue(triple, out var count))
                    counts[triple] = count + 1;
                else
                    counts[triple] = 1;
            }

            var first = true;
            foreach (var (triple, count) in counts.OrderBy(it => it.Key.Item1))
            {
                if (!first)
                    args.Message.PushNewline();
                else
                    first = false;

                var text = Loc.GetString(triple.Item1, ("count", count));
                if (triple.Item2 is { } bleedingMessage)
                    text = Loc.GetString(bleedingMessage, ("wound", text));
                if (triple.Item3 is { } tendedMessage)
                    text = Loc.GetString(tendedMessage, ("wound", text));

                args.Message.AddMarkupOrThrow(Loc.GetString(WoundCountModifier, ("wound", text), ("count", count), ("target", Identity.Entity(ent, EntityManager)), ("organ", organ)));
            }
        }
    }

    public void HealWounds(Entity<WoundableBodyComponent> ent, DamageSpecifier incoming, bool passive, bool refresh)
    {
        var evt = new HealWoundsEvent(incoming, passive);
        RaiseLocalEvent(ent, ref evt);

        if (refresh)
            _woundable.RefreshWounds(ent, false, null);
    }

    private void OnDamageDealt(Entity<WoundableBodyComponent> ent, ref DamageDealtEvent args)
    {
        if (_timing.ApplyingState || !TryComp<DamageableComponent>(ent, out _))
            return;

        if (args.Damage.AnyPositive())
        {
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
            var rand = new System.Random(seed);

            var organs = _woundableOrgan.GetWoundableOrgans(ent, args.TargetZone);
            var target = SharedRandomExtensions.Pick(organs, rand);

            var organEvt = args with { Damage = DamageSpecifier.GetPositive(args.Damage) };
            RaiseLocalEvent(target, ref organEvt);
        }

        if (args.Damage.AnyNegative())
            HealWounds(ent, DamageSpecifier.GetNegative(args.Damage), false, false);

        _woundable.RefreshWounds(ent, args.InterruptsDoAfters, args.Origin);
    }

    private void OnRefreshWounds(Entity<WoundableBodyComponent> ent, ref RefreshWoundsEvent args)
    {
        var damageable = Comp<DamageableComponent>(ent);

        var evt = new WoundGetDamageEvent(new(), null);
        RaiseLocalEvent(ent, ref evt);

        var dict = damageable.Damage.DamageDict;

        var damageDone = new DamageSpecifier();
        foreach (var (type, newValue) in evt.Accumulator.DamageDict)
        {
            var oldValue = dict.GetValueOrDefault(type, FixedPoint2.Zero);

            damageDone.DamageDict[type] = newValue - oldValue;
        }

        damageable.Damage = evt.Accumulator;
        _damageable.OnEntityDamageChanged((ent, damageable), damageDone, args.InterruptsDoAfters, args.Origin);
    }

    public void ClampWounds(Entity<WoundableBodyComponent> ent, float probability)
    {
        var evt = new ClampWoundsEvent(probability);
        RaiseLocalEvent(ent, ref evt);
    }
}
