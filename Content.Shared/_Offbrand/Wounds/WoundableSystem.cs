using System.Linq;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class WoundableSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WoundableBodySystem _woundableBody = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<WoundableComponent, RefreshWoundsEvent>(OnWoundableRefreshWounds);
        SubscribeLocalEvent<WoundableComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<WoundableComponent, BodyRelayedEvent<WoundGetDamageEvent>>(OnGetWoundDamages);

        SubscribeLocalEvent<WoundComponent, StatusEffectRelayedEvent<WoundGetDamageEvent>>(OnWoundGetDamage);
        SubscribeLocalEvent<WoundComponent, StatusEffectRelayedEvent<GetWoundsWithSpaceEvent>>(OnGetWoundsWithSpace);
        SubscribeLocalEvent<WoundComponent, RefreshWoundsEvent>(OnWoundRefreshWounds);
        SubscribeLocalEvent<WoundComponent, StatusEffectRemovedEvent>(OnWoundRemoved);

        SubscribeLocalEvent<PainfulWoundComponent, StatusEffectRelayedEvent<GetPainEvent>>(OnGetPain);
        SubscribeLocalEvent<HealableWoundComponent, StatusEffectRelayedEvent<HealWoundsEvent>>(OnHealHealableWounds);
        SubscribeLocalEvent<BleedingWoundComponent, StatusEffectRelayedEvent<GetBleedLevelEvent>>(OnGetBleedLevel);
        SubscribeLocalEvent<ClampableWoundComponent, StatusEffectRelayedEvent<ClampWoundsEvent>>(OnClampWounds);
    }

    private void OnGetWoundDamages(Entity<WoundableComponent> ent, ref BodyRelayedEvent<WoundGetDamageEvent> args)
    {
        var evt = new WoundGetDamageEvent(new(), new());
        RaiseLocalEvent(ent, ref evt);

        ent.Comp.Damage = evt.Accumulator;
        ent.Comp.TendedDamage = evt.Tended!;
        Dirty(ent);

        var notif = new WoundableDamageChanged();
        RaiseLocalEvent(ent, ref notif);

        foreach (var entry in evt.Accumulator.DamageDict)
        {
            if (!args.Args.Accumulator.DamageDict.TryAdd(entry.Key, entry.Value))
            {
                args.Args.Accumulator.DamageDict[entry.Key] += entry.Value;
            }
        }
    }

    private void OnWoundableRefreshWounds(Entity<WoundableComponent> ent, ref RefreshWoundsEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Body is not { } body)
            return;

        RaiseLocalEvent(body, ref args);
    }

    private void OnWoundRefreshWounds(Entity<WoundComponent> ent, ref RefreshWoundsEvent args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var status) || status.AppliedTo is not { } woundable)
            return;

        RaiseLocalEvent(woundable, ref args);
    }

    private void OnAfterAutoHandleState(Entity<WoundableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var notif = new WoundableDamageChanged();
        RaiseLocalEvent(ent, ref notif);
    }

    private void OnWoundGetDamage(Entity<WoundComponent> ent, ref StatusEffectRelayedEvent<WoundGetDamageEvent> args)
    {
        var accumulator = args.Args.Accumulator;
        var tended = CompOrNull<TendableWoundComponent>(ent)?.Tended ?? false;

        foreach (var (type, value) in ent.Comp.Damage.DamageDict)
        {
            if (accumulator.DamageDict.TryGetValue(type, out var existing))
                accumulator.DamageDict[type] = existing + value;
            else
                accumulator.DamageDict[type] = value;

            if (tended && args.Args.Tended is { } tendedDamage)
            {
                if (tendedDamage.DamageDict.TryGetValue(type, out existing))
                    tendedDamage.DamageDict[type] = existing + value;
                else
                    tendedDamage.DamageDict[type] = value;
            }
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

    private void OnWoundRemoved(Entity<WoundComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Damage.Empty)
            return;

        if (!TryComp<OrganComponent>(args.Target, out var organ))
            return;

        if (organ.Body is { } body)
            RefreshWounds(body, false, null);
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

    /// <summary>
    /// Gets the amount of bleeding this wound causes.
    /// </summary>
    /// <param name="ent">The wound to check</param>
    [PublicAPI]
    public float BleedLevel(Entity<BleedingWoundComponent> ent)
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

    private void OnGetBleedLevel(Entity<BleedingWoundComponent> ent, ref StatusEffectRelayedEvent<GetBleedLevelEvent> args)
    {
        args.Args = args.Args with { BleedLevel = args.Args.BleedLevel + BleedLevel(ent) };
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

    /// <summary>
    /// Adds damage to a wound, without refreshing cached information.
    /// </summary>
    /// <param name="ent">The wound to add damage to.</param>
    /// <param name="specifier">The damage to add.</param>
    [PublicAPI]
    public void AddWoundDamage(Entity<WoundComponent?> ent, DamageSpecifier specifier)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Damage += specifier;
        ent.Comp.WoundedAt = _timing.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Tends the wound with the given damage.
    /// </summary>
    /// <param name="ent">The wound to tend.</param>
    /// <param name="specifier">The amount of damage to heal when tending, if any.</param>
    public void TendWound(Entity<TendableWoundComponent> ent, DamageSpecifier? specifier)
    {
        var wound = Comp<WoundComponent>(ent);

        ent.Comp.Tended = true;
        if (specifier is { } damage)
        {
            wound.Damage.Heal(damage);
            wound.Damage.TrimZeros();
            Dirty(ent.Owner, wound);

            if (wound.Damage.Empty)
                PredictedQueueDel(ent);

            RefreshWounds(ent, false, null);
        }
        Dirty(ent);
    }

    /// <summary>
    /// Sets whether a healable wound can heal.
    /// </summary>
    /// <param name="ent">The wound to adjust.</param>
    [PublicAPI]
    public void SetHealable(Entity<HealableWoundComponent> ent)
    {
        ent.Comp.CanHeal = true;
        Dirty(ent);
    }

    /// <summary>
    /// Refreshes cached data associated with wounds. Can be called on a wound, woundable, or woundable body.
    /// </summary>
    /// <param name="target">The relevant entity to refresh wounds on.</param>
    /// <param name="interruptsDoAfters">Whether this should interrupt do-afters.</param>
    /// <param name="origin">The originating entity that caused the change required for refresh.</param>
    [PublicAPI]
    public void RefreshWounds(EntityUid target, bool interruptsDoAfters, EntityUid? origin)
    {
        var evt = new RefreshWoundsEvent(interruptsDoAfters, origin);
        RaiseLocalEvent(target, ref evt);
    }

    private void OnDamageDealt(Entity<WoundableComponent> ent, ref DamageDealtEvent args)
    {
        foreach (var (type, damage) in args.Damage.DamageDict)
        {
            if (damage <= FixedPoint2.Zero)
                continue;

            var incoming = new DamageSpecifier() { DamageDict = new() { { type, damage } } };

            var evt = new GetWoundsWithSpaceEvent(new(), incoming);
            RaiseLocalEvent(ent, ref evt);

            if (evt.Wounds.Count > 0)
            {
                AddWoundDamage(evt.Wounds[0], incoming);
                continue;
            }

            if (DecideOnWoundType(ent, incoming) is not { } woundToSpawn)
                continue;

            TryWound(ent, woundToSpawn, damage: new(incoming), refresh: false);
        }
    }

    private EntProtoId? DecideOnWoundType(Entity<WoundableComponent> ent, DamageSpecifier damage)
    {
        var pair = damage.DamageDict.MaxBy(kvp => kvp.Value);
        if (!ent.Comp.PotentialWounds.TryGetValue(pair.Key, out var thresholds))
            return null;

        return thresholds.HighestMatch(pair.Value);
    }

    public bool TryWound(Entity<WoundableComponent> target, EntProtoId woundToSpawn, DamageSpecifier? damage = null, bool unique = false, bool refresh = true)
    {
        if (unique && _statusEffects.HasStatusEffect(target, woundToSpawn))
            return false;

        PredictedTrySpawnInContainer(woundToSpawn,
            target,
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
            RefreshWounds(wound.Value, false, null);

        return true;
    }
}
