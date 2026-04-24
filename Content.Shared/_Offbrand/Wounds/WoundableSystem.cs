using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Wounds;

public sealed class WoundableSystem : OffbrandDamageSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WoundableComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<WoundableComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined, before: [typeof(SharedBloodstreamSystem)]);
        SubscribeLocalEvent<WoundableComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<WoundComponent, StatusEffectRelayedEvent<WoundGetDamageEvent>>(OnWoundGetDamage);
        SubscribeLocalEvent<WoundComponent, StatusEffectRelayedEvent<GetWoundsWithSpaceEvent>>(OnGetWoundsWithSpace);
        SubscribeLocalEvent<WoundComponent, StatusEffectRemovedEvent>(OnWoundRemoved);

        SubscribeLocalEvent<PainfulWoundComponent, StatusEffectRelayedEvent<GetPainEvent>>(OnGetPain);
        SubscribeLocalEvent<HealableWoundComponent, StatusEffectRelayedEvent<HealWoundsEvent>>(OnHealHealableWounds);
        SubscribeLocalEvent<BleedingWoundComponent, StatusEffectRelayedEvent<GetBleedLevelEvent>>(OnGetBleedLevel);
        SubscribeLocalEvent<ClampableWoundComponent, StatusEffectRelayedEvent<ClampWoundsEvent>>(OnClampWounds);
    }

    private void OnShutdown(Entity<WoundableComponent> ent, ref ComponentShutdown args)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundComponent>(ent, out var wounds))
            return;

        foreach (var wound in wounds)
        {
            QueueDel(wound);
        }
    }

    public void TryClearAllWounds(EntityUid uid)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundComponent>(uid, out var wounds))
            return;

        foreach (var wound in wounds)
        {
            QueueDel(wound);
        }
    }

    private static readonly LocId WoundCountModifier = "wound-count-modifier";

    private void OnHealthBeingExamined(Entity<WoundableComponent> ent, ref HealthBeingExaminedEvent args)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundDescriptionComponent>(ent, out var wounds))
            return;

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

            if (TryComp<BleedingWoundComponent>(describable, out var bleeding) && BleedLevel((describable.Owner, bleeding)) > 0f)
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

            args.Message.AddMarkupOrThrow(Loc.GetString(WoundCountModifier, ("wound", text), ("count", count), ("target", Identity.Entity(ent, EntityManager))));
        }
    }

    private void OnWoundRemoved(Entity<WoundComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Damage.Empty)
            return;

        RefreshWounds(args.Target, false, null);
    }

    private void OnDamaged(Entity<WoundableComponent, DamageableComponent> ent, DamageSpecifier overall)
    {
        foreach (var (type, damage) in overall.DamageDict)
        {
            var existing = ent.Comp2.Damage.DamageDict.GetValueOrDefault(type, FixedPoint2.Zero);
            var delta = ent.Comp1.MaximumDamage.TryGetValue(type, out var data)
                ? ComputeDelta(existing, existing + damage, data)
                : FixedPoint2.Zero;

            var incoming = new DamageSpecifier() { DamageDict = new() { { type, damage - delta } } };

            var evt = new GetWoundsWithSpaceEvent(new(), incoming);
            RaiseLocalEvent(ent, ref evt);

            if (evt.Wounds.Count > 0)
            {
                AddWoundDamage(evt.Wounds[0], incoming);
                continue;
            }

            if (DecideOnWoundType(incoming) is not { } woundToSpawn)
                continue;

            TryWound(ent, woundToSpawn, damage: new(incoming), refresh: false);
        }
    }

    private void OnMapInit(Entity<WoundableComponent> ent, ref MapInitEvent args)
    {
        var damageable = Comp<DamageableComponent>(ent);
        if (damageable.Damage.AnyPositive())
            OnDamaged((ent, ent, damageable), DamageSpecifier.GetPositive(damageable.Damage));
    }

    public void SetHealable(Entity<HealableWoundComponent> ent)
    {
        ent.Comp.CanHeal = true;
        Dirty(ent);
    }

    public bool TryWound(Entity<WoundableComponent> ent, EntProtoId woundToSpawn, DamageSpecifier? damage = null, bool unique = false, bool refresh = true)
    {
        if (unique && _statusEffects.HasStatusEffect(ent, woundToSpawn))
            return false;

        PredictedTrySpawnInContainer(woundToSpawn,
            ent,
            StatusEffectContainerComponent.ContainerId,
            out var wound);

        DebugTools.Assert(wound is not null, "could not spawn wound in container");
        if (wound is null)
            return false;

        var comp = Comp<WoundComponent>(wound.Value);

        if (damage is not null)
            comp.Damage = damage.Clone();
        comp.WoundedAt = _timing.CurTime;
        comp.CreatedAt = _timing.CurTime;

        Dirty(wound.Value, comp);
        if (refresh)
            RefreshWounds((ent, ent, null), false, null);

        return true;
    }

    public void HealWounds(Entity<WoundableComponent> ent, DamageSpecifier incoming, bool passive, bool refresh)
    {
        var evt = new HealWoundsEvent(incoming, passive);
        RaiseLocalEvent(ent, ref evt);

        if (refresh)
            RefreshWounds((ent, ent, null), false, null);
    }

    /// <param name="current">The current amount of damage</param>
    /// <param name="incoming">The incoming amount of damage, that is, <see cref="current"/> + some damage specifier</param>
    /// <param name="modifier">The modifier to curve damage above the maximum by</param>
    /// <returns>The amount to subtract from the damage specifier added to <see cref="current"/></returns>
    private FixedPoint2 ComputeDelta(FixedPoint2 current, FixedPoint2 incoming, (FixedPoint2 Base, FixedPoint2 Factor) modifier)
    {
        DebugTools.Assert(incoming > 0);

        if (current >= modifier.Base && modifier.Factor != FixedPoint2.Zero)
        {
            var factor = modifier.Factor.Double();
            var @base = modifier.Base.Double();
            Func<FixedPoint2, double> fn = x => Math.Log( Math.Abs(factor - @base + x.Double()) ) * factor;

            var maximumFromNow = FixedPoint2.New(fn(incoming + current) - fn(current));

            return FixedPoint2.Max(incoming - maximumFromNow, FixedPoint2.Zero);
        }
        if (modifier.Factor != FixedPoint2.Zero)
        {
            var delta = FixedPoint2.Max((incoming + current) - modifier.Base, FixedPoint2.Zero);

            if (delta <= 0)
                return delta;

            var adjustedIncoming = incoming - delta;
            var adjustedCurrent = current + adjustedIncoming;
            var adjustedRemainder = incoming - adjustedIncoming;

            return FixedPoint2.Max( delta - ComputeDelta(adjustedCurrent, adjustedRemainder, modifier), FixedPoint2.Zero );
        }

        return FixedPoint2.Max((incoming + current) - modifier.Base, FixedPoint2.Zero);
    }

    private void OnDamageDealt(Entity<WoundableComponent> ent, ref DamageDealtEvent args)
    {
        if (_timing.ApplyingState || !TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (args.Damage.AnyPositive())
            OnDamaged((ent, ent, damageable), DamageSpecifier.GetPositive(args.Damage));

        if (args.Damage.AnyNegative())
            HealWounds(ent, DamageSpecifier.GetNegative(args.Damage), false, false);

        RefreshWounds((ent, ent, null), args.InterruptsDoAfters, args.Origin);
    }

    private void RefreshWounds(Entity<WoundableComponent?, DamageableComponent?> ent, bool interruptsDoAfters, EntityUid? origin)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var evt = new WoundGetDamageEvent(new());
        RaiseLocalEvent(ent, ref evt);

        var dict = ent.Comp2.Damage.DamageDict;

        var damageDone = new DamageSpecifier();
        foreach (var (type, newValue) in evt.Accumulator.DamageDict)
        {
            var oldValue = dict.GetValueOrDefault(type, FixedPoint2.Zero);

            damageDone.DamageDict[type] = newValue - oldValue;
        }

        ent.Comp2.Damage = evt.Accumulator;
        _damageable.OnEntityDamageChanged((ent, ent.Comp2), damageDone, interruptsDoAfters, origin);
    }

    private void OnHealHealableWounds(Entity<HealableWoundComponent> ent, ref StatusEffectRelayedEvent<HealWoundsEvent> args)
    {
        if (!ent.Comp.CanHeal)
            return;

        var comp = Comp<WoundComponent>(ent);

        if (args.Args.Passive)
        {
            if (comp.Damage.GetTotal() >= ent.Comp.RequiresTendingAbove &&
                !(TryComp<TendableWoundComponent>(ent, out var tendable) && tendable.Tended))
            {
                return;
            }
        }

        args.Args = args.Args with { Damage = comp.Damage.Heal(args.Args.Damage) };

        comp.Damage.TrimZeros();
        args.Args.Damage.TrimZeros();

        Dirty(ent.Owner, comp);

        if (comp.Damage.Empty)
        {
            // use PredictedQueueDel when https://github.com/space-wizards/RobustToolbox/issues/6153 is fixed
            if (_net.IsServer)
                QueueDel(ent.Owner);
        }
    }

    private float BleedLevel(Entity<BleedingWoundComponent> ent)
    {
        var wound = Comp<WoundComponent>(ent);

        if (TryComp<TendableWoundComponent>(ent, out var tendable) && tendable.Tended)
            return 0f;

        if (TryComp<ClampableWoundComponent>(ent, out var clampable) && clampable.Clamped)
            return 0f;

        if (wound.Damage.GetTotal() < ent.Comp.StartsBleedingAbove)
            return 0f;

        var ratio = 1f;
        if (wound.Damage.GetTotal() < ent.Comp.RequiresTendingAbove)
        {
            var expiresAfter = TimeSpan.Zero;

            foreach (var (type, value) in wound.Damage.DamageDict)
            {
                if (ent.Comp.BleedingDurationCoefficients.TryGetValue(type, out var coefficient))
                    expiresAfter += TimeSpan.FromSeconds((value * coefficient).Double());
            }

            if (wound.CreatedAt + expiresAfter <= _timing.CurTime)
                return 0f;

            var expiryTime = ((wound.CreatedAt + expiresAfter) - _timing.CurTime).TotalSeconds;
            ratio = (float)(expiryTime / expiresAfter.TotalSeconds);
        }

        var bleedAddition = 0f;

        foreach (var (type, value) in wound.Damage.DamageDict)
        {
            if (ent.Comp.BleedingCoefficients.TryGetValue(type, out var coefficient))
                bleedAddition += value.Float() * coefficient;
        }

        return bleedAddition * ratio;
    }

    public void ClampWounds(Entity<WoundableComponent> ent, float probability)
    {
        var evt = new ClampWoundsEvent(probability);
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnClampWounds(Entity<ClampableWoundComponent> ent, ref StatusEffectRelayedEvent<ClampWoundsEvent> args)
    {
        if (ent.Comp.Clamped)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        if (!rand.Prob(args.Args.Probability))
            return;

        ent.Comp.Clamped = true;
        Dirty(ent);
    }

    private void OnGetBleedLevel(Entity<BleedingWoundComponent> ent, ref StatusEffectRelayedEvent<GetBleedLevelEvent> args)
    {
        args.Args = args.Args with { BleedLevel = args.Args.BleedLevel + BleedLevel(ent) };
    }

    private void OnWoundGetDamage(Entity<WoundComponent> ent, ref StatusEffectRelayedEvent<WoundGetDamageEvent> args)
    {
        var accumulator = args.Args.Accumulator;

        foreach (var (type, value) in ent.Comp.Damage.DamageDict)
        {
            if (accumulator.DamageDict.TryGetValue(type, out var existing))
                accumulator.DamageDict[type] = existing + value;
            else
                accumulator.DamageDict[type] = value;
        }
    }

    private void OnGetWoundsWithSpace(Entity<WoundComponent> ent, ref StatusEffectRelayedEvent<GetWoundsWithSpaceEvent> args)
    {
        if (ent.Comp.Damage.GetTotal() + args.Args.Damage.GetTotal() > ent.Comp.MaximumDamage)
            return;
        if (ent.Comp.Damage.Empty) // this is the client hack for deletion being quirky
            return;

        var ourPair = ent.Comp.Damage.DamageDict.MaxBy(kvp => kvp.Value);
        var theirPair = args.Args.Damage.DamageDict.MaxBy(kvp => kvp.Value);

        if (ourPair.Key != theirPair.Key)
            return;

        args.Args.Wounds.Add(ent);
    }

    private void OnGetPain(Entity<PainfulWoundComponent> ent, ref StatusEffectRelayedEvent<GetPainEvent> args)
    {
        var wound = Comp<WoundComponent>(ent);
        var damage = wound.Damage.DamageDict;
        var lastingPain = FixedPoint2.Zero;
        var freshPain = FixedPoint2.Zero;

        foreach (var (type, value) in damage)
        {
            if (ent.Comp.PainCoefficients.TryGetValue(type, out var coefficient))
                lastingPain += coefficient * value;
            if (ent.Comp.FreshPainCoefficients.TryGetValue(type, out var freshCoefficient))
                freshPain += freshCoefficient * value;
        }

        var delta = _timing.CurTime - wound.WoundedAt;

        freshPain = FixedPoint2.Max(0, freshPain - (delta.TotalSeconds * ent.Comp.FreshPainDecreasePerSecond));

        args.Args = args.Args with { Pain = args.Args.Pain + lastingPain + freshPain };
    }

    private void AddWoundDamage(Entity<WoundComponent?> ent, DamageSpecifier specifier)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Damage += specifier;
        ent.Comp.WoundedAt = _timing.CurTime;
        Dirty(ent);
    }

    public void TendWound(Entity<WoundableComponent?> woundable, Entity<TendableWoundComponent> ent, DamageSpecifier? specifier)
    {
        var wound = Comp<WoundComponent>(ent);

        ent.Comp.Tended = true;
        if (specifier is { } damage)
        {
            var remainder = wound.Damage.Heal(damage);
            wound.Damage.TrimZeros();
            Dirty(ent.Owner, wound);

            if (wound.Damage.Empty)
                PredictedQueueDel(ent);

            RefreshWounds((woundable, woundable.Comp, null), false, null);
        }
        Dirty(ent);
    }

    private static readonly EntProtoId WoundCutMassive = "WoundCutMassive";
    private static readonly EntProtoId WoundCutSevere = "WoundCutSevere";
    private static readonly EntProtoId WoundCutModerate = "WoundCutModerate";
    private static readonly EntProtoId WoundCutSmall = "WoundCutSmall";

    private static readonly EntProtoId WoundPunctureMassive = "WoundPunctureMassive";
    private static readonly EntProtoId WoundPunctureSevere = "WoundPunctureSevere";
    private static readonly EntProtoId WoundPunctureModerate = "WoundPunctureModerate";
    private static readonly EntProtoId WoundPunctureSmall = "WoundPunctureSmall";

    private static readonly EntProtoId WoundHeatCarbonized = "WoundHeatCarbonized";
    private static readonly EntProtoId WoundHeatSevere = "WoundHeatSevere";
    private static readonly EntProtoId WoundHeatModerate = "WoundHeatModerate";
    private static readonly EntProtoId WoundHeatSmall = "WoundHeatSmall";

    private static readonly EntProtoId WoundColdPetrified = "WoundColdPetrified";
    private static readonly EntProtoId WoundColdSevere = "WoundColdSevere";
    private static readonly EntProtoId WoundColdModerate = "WoundColdModerate";
    private static readonly EntProtoId WoundColdSmall = "WoundColdSmall";

    private static readonly EntProtoId WoundCausticSloughing = "WoundCausticSloughing";
    private static readonly EntProtoId WoundCausticSevere = "WoundCausticSevere";
    private static readonly EntProtoId WoundCausticModerate = "WoundCausticModerate";
    private static readonly EntProtoId WoundCausticSmall = "WoundCausticSmall";

    private static readonly EntProtoId WoundShockExploded = "WoundShockExploded";
    private static readonly EntProtoId WoundShockSevere = "WoundShockSevere";
    private static readonly EntProtoId WoundShockModerate = "WoundShockModerate";
    private static readonly EntProtoId WoundShockSmall = "WoundShockSmall";

    private static readonly EntProtoId WoundBruise = "WoundBruise";

    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";
    private static readonly ProtoId<DamageTypePrototype> Piercing = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";
    private static readonly ProtoId<DamageTypePrototype> Cold = "Cold";
    private static readonly ProtoId<DamageTypePrototype> Caustic = "Caustic";
    private static readonly ProtoId<DamageTypePrototype> Shock = "Shock";

    private static EntProtoId? DecideOnWoundType(DamageSpecifier damage)
    {
        var pair = damage.DamageDict.MaxBy(kvp => kvp.Value);

        if (pair.Key == Blunt)
        {
            return WoundBruise;
        }
        else if (pair.Key == Slash)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundCutMassive,
                >= 20d => WoundCutSevere,
                >= 10d => WoundCutModerate,
                _ => WoundCutSmall,
            };
        }
        else if (pair.Key == Piercing)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundPunctureMassive,
                >= 20d => WoundPunctureSevere,
                >= 10d => WoundPunctureModerate,
                _ => WoundPunctureSmall,
            };
        }
        else if (pair.Key == Heat)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundHeatCarbonized,
                >= 20d => WoundHeatSevere,
                >= 10d => WoundHeatModerate,
                _ => WoundHeatSmall,
            };
        }
        else if (pair.Key == Cold)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundColdPetrified,
                >= 20d => WoundColdSevere,
                >= 10d => WoundColdModerate,
                _ => WoundColdSmall,
            };
        }
        else if (pair.Key == Caustic)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundCausticSloughing,
                >= 20d => WoundCausticSevere,
                >= 10d => WoundCausticModerate,
                _ => WoundCausticSmall,
            };
        }
        else if (pair.Key == Shock)
        {
            return pair.Value.Double() switch {
                >= 30d => WoundShockExploded,
                >= 20d => WoundShockSevere,
                >= 10d => WoundShockModerate,
                _ => WoundShockSmall,
            };
        }

        return null;
    }
}
