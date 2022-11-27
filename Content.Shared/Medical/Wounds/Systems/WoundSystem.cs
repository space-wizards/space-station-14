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
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableCreated);
    }

    //TODO: Smug will this break networking? - Jez
    private void OnWoundableCreated(EntityUid uid, WoundableComponent component, ComponentInit args)
    {
        if (component.Health < 0)//if initial woundable health isn't defined default to the woundCap.
        {
            component.Health = component.HealthCap;
        }
    }

    private void OnBodyAttacked(EntityUid uid, BodyComponent component, AttackedEvent args)
    {
        if (!TryComp(args.Used, out TraumaInflicterComponent? inflicter))
            return;

        var parts = _body.GetBodyChildren(uid, component).ToList();
        var part = _random.Pick(parts);
        TryApplyTrauma(part.Id, inflicter);
    }

    private void CacheData()
    {
        _cachedWounds.Clear();

        foreach (var traumaType in _prototypeManager.EnumeratePrototypes<TraumaPrototype>())
        {
            _cachedWounds.Add(traumaType.ID, new WoundTable(traumaType));
        }
    }

    public bool TryApplyTrauma(EntityUid target, TraumaInflicterComponent inflicter)
    {
        var success = false;
        foreach (var (traumaType, trauma) in inflicter.Traumas)
        {
            success |= ApplyTraumaToPart(target, traumaType, trauma);

            // TODO wounds before merge add support for non penetrating traumas
            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenetrationChance > 0 && !_random.Prob(trauma.PenetrationChance.Float()))
                continue;

            success |= ApplyTraumaToPart(target, type, trauma);
        }

        return success;
    }

    public bool ApplyTraumaToPart(EntityUid target, string traumaType, TraumaDamage traumaDamage)
    {
        // TODO wounds before merge turn into tryget
        if (GetValidWoundable(target, traumaType) is not {Target: var woundableId, Woundable: var woundable})
            return false;

        var modifiedDamage = ApplyTraumaModifiers(traumaType, woundable.TraumaResistance, traumaDamage.Damage);
        return TryChooseWound(traumaType, modifiedDamage, out var woundId) &&
               AddWound(woundableId, woundId, woundable) && ApplyRawWoundDamage(woundableId, modifiedDamage, woundable);
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
        ApplyRawIntegrityDamage(woundableId, wound.IntegrityDamage, woundable);
        return true;
    }

    public bool ApplyRawWoundDamage(EntityUid woundableId, FixedPoint2 woundDamage,WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        woundable.HealthCapDamage += woundDamage;
        if (woundable.HealthCapDamage < FixedPoint2.Zero)
            ApplyRawIntegrityDamage(woundableId, -woundable.HealthCapDamage, woundable);
        woundable.HealthCapDamage = 0;
        return true;
    }

    public bool ApplyRawIntegrityDamage(EntityUid woundableId, FixedPoint2 integrityDamage, WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        woundable.Integrity -= integrityDamage;
        if (woundable.Integrity <= FixedPoint2.Zero)
            DestroyWoundable(woundableId);
        return true;
    }

    public bool IncreaseWoundSeverity(EntityUid woundableId, EntityUid woundId, FixedPoint2 severityIncrease,WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        ApplyWoundSeverityDelta(woundable, wound,wound.SeverityPercentage + severityIncrease);
        return true;
    }

    public bool DecreaseWoundSeverity(EntityUid woundableId, EntityUid woundId, FixedPoint2 severityDecrease,WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        ApplyWoundSeverityDelta(woundable, wound,wound.SeverityPercentage - severityDecrease);
        return true;
    }

    public bool SetWoundSeverity(EntityUid woundableId, EntityUid woundId, FixedPoint2 severityAmount,WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        ApplyWoundSeverityDelta(woundable, wound,severityAmount);
        return true;
    }

    private void ApplyWoundSeverityDelta(WoundableComponent woundable, WoundComponent wound, FixedPoint2 newSeverity)
    {
        newSeverity = Math.Clamp(newSeverity.Float(), 0.0f, 1.0f);
        if (wound.SeverityPercentage == newSeverity)
            return;
        var severityDelta = newSeverity - wound.SeverityPercentage;
        var healthCapDamageDelta = severityDelta * wound.HealthCapDamage;
        woundable.HealthCapDamage += healthCapDamageDelta;
    }

    public bool RemoveWound(EntityUid woundableId, EntityUid woundId, WoundableComponent? woundable = null, WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        var woundContainer = _containers.GetContainer(woundableId, WoundContainerId);
        if (!woundContainer.Remove(woundId))
            return false;
        ApplyWoundSeverityDelta(woundable, wound, 0);
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
