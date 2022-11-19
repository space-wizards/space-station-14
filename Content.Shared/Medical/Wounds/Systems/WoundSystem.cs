using System.Diagnostics.CodeAnalysis;
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
    private const string WoundContainerId = "WoundSystemWounds";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private readonly Dictionary<string, WoundTable> _cachedWounds = new();

    public override void Initialize()
    {
        CacheData();
        _prototypeManager.PrototypesReloaded += _ => CacheData();

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

    private void CacheData()
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
            success |= AddWoundToPart(target, traumaType, trauma);

            // TODO wounds before merge add support for non penetrating traumas
            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenetrationChance > 0 && !_random.Prob(trauma.PenetrationChance.Float()))
                continue;

            success |= AddWoundToPart(target, type, trauma);
        }

        return success;
    }

    public bool AddWoundToPart(EntityUid target, string traumaType, TraumaDamage traumaDamage)
    {
        // TODO wounds before merge turn into tryget
        if (GetValidWoundable(target, traumaType) is not {Target: var woundableId, Woundable: var woundable})
            return false;

        var modifiedDamage = ApplyTraumaModifiers(traumaType, woundable.TraumaResistance, traumaDamage.Damage);

        return TryChooseWound(traumaType, modifiedDamage, out var woundId) &&
               AddWound(woundableId, woundId, woundable);
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

    private bool AddWound(EntityUid woundableId, string woundPrototypeId, WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;

        var woundId = Spawn(woundPrototypeId, woundableId.ToCoordinates());
        var wound = Comp<WoundComponent>(woundId);

        var wounds = _containers.EnsureContainer<Container>(woundableId, WoundContainerId);
        if (!wounds.Insert(woundId))
            return false;

        var healthDamage = wound.HealthDamage;
        var integrityDamage = wound.IntegrityDamage;
        var overflowDamage = FixedPoint2.Zero;

        if (healthDamage > woundable.Health)
        {
            integrityDamage += healthDamage - woundable.Health;
            healthDamage = woundable.Health;
        }

        if (integrityDamage > woundable.Integrity)
        {
            overflowDamage += integrityDamage - woundable.Integrity;
            integrityDamage = woundable.Integrity;
        }

        woundable.Health -= healthDamage;
        woundable.Integrity -= integrityDamage;

        wound.HealthDamageDealt = healthDamage;
        wound.IntegrityDamageDealt = integrityDamage;
        wound.OverflowDamageDealt = overflowDamage;

        if (woundable.Health + woundable.Integrity <= FixedPoint2.Zero)
            DestroyWoundable(woundableId);

        return true;
    }

    private void DestroyWoundable(EntityUid woundableId, WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return;

        if (woundable.DestroyWoundId != null)
        {
            if (_body.TryGetPartParentPart(woundableId, out var parent))
            {
                AddWound(parent, woundable.DestroyWoundId);
                // TODO wounds before merge gib
                _body.DropPart(woundableId);
            }
            else
            {
                _body.GibBody(woundableId);
            }
        }

        var ev = new WoundableDestroyedEvent();
        RaiseLocalEvent(woundableId, ev);
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

    private bool TryChooseWound(string traumaType, FixedPoint2 traumaDamage, [NotNullWhen(true)] out string? woundId)
    {
        foreach (var woundData in _cachedWounds[traumaType].HighestToLowest())
        {
            if (woundData.Key > traumaDamage)
                continue;

            woundId = woundData.Value;
            return true;
        }

        woundId = null;
        return false;
    }

    private (EntityUid Target, WoundableComponent Woundable)? FindValidWoundableInAdjacent(EntityUid target, string traumaType)
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
