using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    private const string WoundContainerId = "WoundableComponentWounds";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

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

        var parts = _body.GetBodyChildren(uid, component).ToList();
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
            success |= TryAddWound(target, traumaType, trauma);

            // TODO wounds before merge add support for non penetrating traumas
            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenetrationChance > 0 && !_random.Prob(trauma.PenetrationChance.Float()))
                continue;

            success |= TryAddWound(target, type, trauma);
        }

        return success;
    }

    public bool TryAddWound(EntityUid target, string traumaType, TraumaDamage traumaDamage)
    {
        // TODO wounds before merge turn into tryget
        if (GetValidWoundable(target, traumaType) is not {Woundable: var woundable})
            return false;

        var modifiers = ApplyTraumaModifiers(traumaType, woundable.TraumaResistance, traumaDamage.Damage);

        return TryCreateWound(target, traumaType, modifiers, out var wound) &&
               AddWound(woundable, wound.Id, wound.Component);
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

    private bool AddWound(WoundableComponent woundable, EntityUid woundId, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;

        var wounds = _containers.EnsureContainer<Container>(woundable.Owner, WoundContainerId);
        return wounds.Insert(woundId);
    }

    private (EntityUid Target, WoundableComponent Woundable)? GetValidWoundable(EntityUid target, string traumaType)
    {
        if (!TryComp<WoundableComponent>(target, out var woundable))
            return null;

        if (woundable.AllowedTraumaTypes?.Contains(traumaType) ?? false)
            return (target, woundable);

        var adjacentWoundable = FindValidWoundableInAdjacent(target, traumaType);
        return adjacentWoundable;
    }

    private bool TryCreateWound(
        EntityUid woundableId,
        string traumaType,
        FixedPoint2 traumaDamage,
        out (EntityUid Id, WoundComponent Component) wound)
    {
        foreach (var woundData in _cachedWounds[traumaType].HighestToLowest())
        {
            if (woundData.Key > traumaDamage)
                continue;

            // TODO wounds before merge dont spawn on the client
            var woundId = Spawn(woundData.Value, woundableId.ToCoordinates());
            var woundComponent = Comp<WoundComponent>(woundId);
            wound = (woundId, woundComponent);
            return true;
        }

        wound = default;
        return false;
    }

    private (EntityUid Target, WoundableComponent Woundable)? FindValidWoundableInAdjacent(EntityUid target,
        string traumaType)
    {
        //search all the children in the body for a part that accepts the wound we want to apply
        //checks organs first, then recursively checks child parts and their organs.
        var foundContainer = FindValidWoundableInOrgans(target, traumaType) ??
                             FindValidWoundableInAdjacentParts(target, traumaType);
        return foundContainer;
    }

    private (EntityUid Target, WoundableComponent Woundable)? FindValidWoundableInAdjacentParts(EntityUid target,
        string traumaType)
    {
        foreach (var data in _body.GetBodyPartAdjacentPartsComponents<WoundableComponent>(target))
        {
            if (data.Component.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (target, data.Component);
        }

        return null;
    }

    private (EntityUid Target, WoundableComponent Woundable)? FindValidWoundableInOrgans(EntityUid target,
        string traumaType)
    {
        foreach (var data in _body.GetBodyPartOrganComponents<WoundableComponent>(target))
        {
            if (data.Comp.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (target, data.Comp);
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
