using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Prototypes;
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

public sealed class WoundableSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WoundableComponent, BeforeDamageCommitEvent>(OnBeforeDamageCommit);
        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnDamageChanged);
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

    private void ValidateWounds(EntityUid ent, DamageSpecifier? incoming)
    {
#if DEBUG
        var damageable = Comp<DamageableComponent>(ent);

        var evt = new WoundGetDamageEvent(new());
        RaiseLocalEvent(ent, ref evt);

        foreach (var (type, currentValue) in damageable.Damage.DamageDict)
        {
            if (!evt.Accumulator.DamageDict.TryGetValue(type, out var expectedValue))
                continue;

            if (incoming is not null && incoming.DamageDict.TryGetValue(type, out var delta) && delta <= 0)
            {
                DebugTools.AssertEqual(currentValue + delta, expectedValue, $"wounds and damageable after delta don't line up for {type}");
            }
            else
            {
                DebugTools.AssertEqual(currentValue, expectedValue, $"wounds and damageable don't line up for {type}");
            }
        }
#endif
    }

    private void OnBeforeDamageCommit(Entity<WoundableComponent> ent, ref BeforeDamageCommitEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var damageable = Comp<DamageableComponent>(ent);

        if (args.Damage.AnyNegative() && !args.ForceRefresh)
            OnHealed(ent, DamageSpecifier.GetNegative(args.Damage));

        var evt = new WoundGetDamageEvent(new());
        RaiseLocalEvent(ent, ref evt);

        var minimumDamage = evt.Accumulator;

        var dict = damageable.Damage.DamageDict;

        var hasCloned = false;
        foreach (var (type, minimumValue) in minimumDamage.DamageDict)
        {
            var deltaValue = args.Damage.DamageDict.GetValueOrDefault(type, FixedPoint2.Zero);
            var oldValue = dict.GetValueOrDefault(type, FixedPoint2.Zero);

            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + deltaValue);

            var delta = newValue - minimumValue;

            if (delta >= 0)
                continue;

            if (!hasCloned)
            {
                hasCloned = true;
                args.Damage = new(args.Damage);
            }

            args.Damage.DamageDict[type] = deltaValue - delta;
        }

        if (!args.ForceRefresh)
            ValidateWounds(ent, args.Damage);
    }

    private void OnWoundRemoved(Entity<WoundComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Damage.Empty)
            return;

        _damageable.TryChangeDamage(args.Target, -ent.Comp.Damage.ToSpecifier(), true, false, null, null, forceRefresh: true);
        ValidateWounds(args.Target, null);
    }

    private void OnDamaged(Entity<WoundableComponent> ent, DamageSpecifier overall)
    {
        foreach (var (type, damage) in overall.DamageDict)
        {
            var incoming = new DamageSpecifier() { DamageDict = new() { { type, damage } } };

            var evt = new GetWoundsWithSpaceEvent(new(), incoming);
            RaiseLocalEvent(ent, ref evt);

            if (evt.Wounds.Count > 0)
            {
                AddWoundDamage(evt.Wounds[0], incoming);
                continue;
            }

            if (DecideOnWoundType(incoming) is not { } woundToSpawn)
                continue;

            TryWound(ent, woundToSpawn, damage: new(incoming));
        }
    }

    private void OnMapInit(Entity<WoundableComponent> ent, ref MapInitEvent args)
    {
        var damageable = Comp<DamageableComponent>(ent);
        if (damageable.Damage.AnyPositive())
            OnDamaged(ent, DamageSpecifier.GetPositive(damageable.Damage));
    }

    public void SetHealable(Entity<HealableWoundComponent> ent)
    {
        ent.Comp.CanHeal = true;
        Dirty(ent);
    }

    public bool TryWound(Entity<WoundableComponent> ent, EntProtoId woundToSpawn, Damages? damage = null, bool unique = false, bool refreshDamage = false)
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

        if (refreshDamage)
            _damageable.TryChangeDamage(ent.Owner, new(), true, true, null, null, forceRefresh: true);

        Dirty(wound.Value, comp);
        return true;
    }

    private void OnHealed(Entity<WoundableComponent> ent, DamageSpecifier incoming)
    {
        var evt = new HealWoundsEvent(incoming);
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnDamageChanged(Entity<WoundableComponent> ent, ref DamageChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.DamageDelta is not { } delta || args.ForcedRefresh)
            return;

        if (delta.AnyPositive())
            OnDamaged(ent, DamageSpecifier.GetPositive(delta));

        ValidateWounds(ent, null);
    }

    private void OnHealHealableWounds(Entity<HealableWoundComponent> ent, ref StatusEffectRelayedEvent<HealWoundsEvent> args)
    {
        if (!ent.Comp.CanHeal)
            return;

        var comp = Comp<WoundComponent>(ent);

        args.Args = args.Args with { Damage = comp.Damage.Heal(args.Args.Damage).ToSpecifier() };

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

    public void ClampWounds(Entity<WoundableComponent?> ent, float probability)
    {
        var evt = new ClampWoundsEvent(probability);
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnClampWounds(Entity<ClampableWoundComponent> ent, ref StatusEffectRelayedEvent<ClampWoundsEvent> args)
    {
        if (ent.Comp.Clamped)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
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

    public void AddWoundDamage(Entity<WoundComponent?> ent, DamageSpecifier specifier)
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

            var changeBy = damage - remainder.ToSpecifier();
            changeBy.TrimZeros();
            if (changeBy.AnyNegative())
            {
                var actualDelta = _damageable.TryChangeDamage(woundable, changeBy, true, false, null, null, forceRefresh: true);
                DebugTools.Assert(actualDelta is not null);
                DebugTools.Assert(changeBy.Equals(actualDelta!), $"{changeBy} == {actualDelta!}");
            }

            ValidateWounds(woundable, null);
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
