using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

//TODO: Convert to use entity hierarchies instead of containers to store wounds
public sealed partial class WoundSystem
{
    private readonly Dictionary<string, WoundTable> _cachedWounds = new();

    private void CacheWoundData()
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
        wound.Parent = woundableId;
        woundable.HealthCapDamage += wound.SeverityPercentage * wound.HealthCapDamage;
        ApplyRawIntegrityDamage(woundableId, wound.IntegrityDamage, woundable);
        return true;
    }

    public bool ApplyRawWoundDamage(EntityUid woundableId, FixedPoint2 woundDamage,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        woundable.HealthCapDamage += woundDamage;
        if (woundable.HealthCapDamage < FixedPoint2.Zero)
            ApplyRawIntegrityDamage(woundableId, -woundable.HealthCapDamage, woundable);
        woundable.HealthCapDamage = 0;
        return true;
    }

    public bool ApplyRawIntegrityDamage(EntityUid woundableId, FixedPoint2 integrityDamage,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        woundable.Integrity -= integrityDamage;
        if (woundable.Integrity <= FixedPoint2.Zero)
            DestroyWoundable(woundableId);
        return true;
    }

    public bool AddWoundSeverity(EntityUid woundableId, EntityUid woundId, float severityIncrease,
        WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        UpdateWoundSeverity(woundable, wound, wound.SeverityPercentage + severityIncrease);
        return true;
    }

    public bool SetWoundSeverity(EntityUid woundableId, EntityUid woundId, float severityAmount,
        WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        UpdateWoundSeverity(woundable, wound, severityAmount);
        return true;
    }

    private void UpdateWoundSeverity(WoundableComponent woundable, WoundComponent wound, float newSeverity)
    {
        newSeverity = Math.Clamp(newSeverity, 0.0f, 1.0f);
        var severityDelta = newSeverity - wound.SeverityPercentage;
        if (severityDelta == 0)
            return;
        var healthCapDamageDelta = severityDelta * wound.HealthCapDamage;
        woundable.HealthCapDamage += healthCapDamageDelta;
        wound.SeverityPercentage = newSeverity;
    }

    public bool RemoveWound(EntityUid woundableId, EntityUid woundId,bool makeScar = false, WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        var woundContainer = _containers.GetContainer(woundableId, WoundContainerId);
        if (!woundContainer.Remove(woundId))
            return false;
        wound.Parent = EntityUid.Invalid;
        UpdateWoundSeverity(woundable, wound, 0);
        if (makeScar && wound.ScarWound != null)
        {
            AddWound(woundableId, wound.ScarWound, woundable);
        }
        EntityManager.DeleteEntity(woundId);
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
