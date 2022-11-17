using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    private readonly Dictionary<string, WoundTable> _cachedWounds = new();

    public override void Initialize()
    {
        CacheData(null);
        _prototypeManager.PrototypesReloaded += CacheData;

        SubscribeLocalEvent<BodyComponent, AttackedEvent>(OnBodyAttacked);
    }

    private void OnBodyAttacked(EntityUid uid, BodyComponent component, AttackedEvent args)
    {
        if (!TryComp(args.Used, out TraumaInflicterComponent? inflicter))
            return;

        var parts = _bodySystem.GetBodyChildren(uid, component).ToList();
        var part = _random.Pick(parts);
        TryApplyWound(part.Id, inflicter);
    }

    private void CacheData(PrototypesReloadedEventArgs? prototypesReloadedEventArgs)
    {
        _cachedWounds.Clear();

        foreach (var traumaType in _prototypeManager.EnumeratePrototypes<TraumaPrototype>())
        {
            _cachedWounds.Add(traumaType.ID, new WoundTable(traumaType));
        }
    }

    public bool TryApplyWound(EntityUid target, TraumaInflicterComponent inflicter)
    {
        var success = false;
        foreach (var (traumaType, trauma) in inflicter.Traumas)
        {
            var validTarget = GetValidWoundable(target, traumaType);

            if (!validTarget.HasValue)
                return false;

            var woundContainer = validTarget.Value.Woundable;
            target = validTarget.Value.Target;

            var modifiers = ApplyTraumaModifiers(traumaType, woundContainer.TraumaResistance, trauma.Damage);
            if (TryPickWound(traumaType, modifiers, out var wound))
                success |= AddWound(target, woundContainer, traumaType, wound);

            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenetrationChance > 0 && !_random.Prob(trauma.PenetrationChance.Float()))
                continue;

            validTarget = GetValidWoundable(target, type);

            if (!validTarget.HasValue)
                continue;

            //Apply penetrating wounds
            woundContainer = validTarget.Value.Woundable;
            target = validTarget.Value.Target;

            modifiers = ApplyTraumaModifiers(type, woundContainer.TraumaResistance, trauma.Damage);
            if (TryPickWound(type, modifiers, out wound))
                success |= AddWound(target, woundContainer, type, wound);
        }

        return success;
    }

    public bool TryAddWound(EntityUid target, string traumaType, TraumaDamage damage)
    {
        var checkedContainer = GetValidWoundable(target, traumaType);

        if (checkedContainer == null)
            return false;

        var woundContainer = checkedContainer.Value.Woundable;
        target = checkedContainer.Value.Target;

        return TryPickWound(traumaType, damage.Damage, out var wound) &&
               AddWound(target, woundContainer, traumaType, wound);
    }

    private FixedPoint2 ApplyTraumaModifiers(string traumaType, TraumaModifierSet? modifiers, FixedPoint2 damage)
    {
        if (!modifiers.HasValue)
            return damage;

        if (modifiers.Value.Coefficients.TryGetValue(traumaType, out var coefficient))
            damage *= coefficient;

        if (modifiers.Value.FlatReduction.TryGetValue(traumaType, out var reduction))
            damage -= reduction;

        return damage;
    }

    private bool AddWound(EntityUid target, WoundableComponent woundContainer, string traumaType, Wound wound)
    {
        woundContainer.Wounds ??= new List<Wound>();
        woundContainer.Wounds.Add(wound);

        return true;
    }

    private (WoundableComponent Woundable, EntityUid Target)? GetValidWoundable(EntityUid target, string traumaType)
    {
        if (!TryComp<WoundableComponent>(target, out var woundable))
            return null;

        if (woundable.AllowedTraumaTypes?.Contains(traumaType) ?? false)
            return (woundable, target);

        var adjacentWoundable = FindValidWoundableInAdjacent(target, traumaType);
        return adjacentWoundable;
    }

    private bool TryPickWound(string traumaType, FixedPoint2 trauma, out Wound wound)
    {
        foreach (var woundData in _cachedWounds[traumaType].HighestToLowest())
        {
            if (woundData.Key > trauma)
                continue;

            wound = new Wound(woundData.Value);
            return true;
        }

        wound = default;
        return false;
    }

    private (WoundableComponent Woundable, EntityUid Target)? FindValidWoundableInAdjacent(EntityUid target,
        string traumaType)
    {
        //search all the children in the body for a part that accepts the wound we want to apply
        //checks organs first, then recursively checks child parts and their organs.
        var foundContainer = FindValidWoundableInOrgans(target, traumaType) ??
                             FindValidWoundableInAdjacentParts(target, traumaType);
        return foundContainer;
    }

    private (WoundableComponent Woundable, EntityUid Target)? FindValidWoundableInAdjacentParts(EntityUid target,
        string traumaType)
    {
        foreach (var data in _bodySystem.GetBodyPartAdjacentPartsComponents<WoundableComponent>(target))
        {
            if (data.Component.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (data.Component, target);
        }

        return null;
    }

    private (WoundableComponent Woundable, EntityUid Target)? FindValidWoundableInOrgans(EntityUid target,
        string traumaType)
    {
        foreach (var data in _bodySystem.GetBodyPartOrganComponents<WoundableComponent>(target))
        {
            if (data.Comp.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (data.Comp, target);
        }

        return null;
    }

    public FixedPoint2 ApplyModifiers(string type, FixedPoint2 damage, TraumaModifierSet modifiers)
    {
        if (modifiers.Coefficients.TryGetValue(type, out var coefficient))
        {
            damage *= coefficient;
        }

        if (modifiers.FlatReduction.TryGetValue(type, out var flat))
        {
            damage -= flat;
        }

        return FixedPoint2.Max(damage, FixedPoint2.Zero);
    }

    private readonly struct WoundTable
    {
        private readonly SortedDictionary<FixedPoint2, string> _wounds;

        public WoundTable(TraumaPrototype trauma)
        {
            _wounds = trauma.Wounds;
        }

        public IEnumerable<KeyValuePair<FixedPoint2, string>> HighestToLowest()
        {
            return _wounds.Reverse();
        }
    }
}
