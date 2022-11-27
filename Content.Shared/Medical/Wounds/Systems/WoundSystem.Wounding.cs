using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
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

        SubscribeLocalEvent<WoundComponent, ComponentGetState>(OnWoundGetState);
        SubscribeLocalEvent<WoundComponent, ComponentHandleState>(OnWoundHandleState);
    }

    private void OnWoundGetState(EntityUid uid, WoundComponent wound, ref ComponentGetState args)
    {
        args.State = new WoundComponentState(wound.Parent);
    }

    private void OnWoundHandleState(EntityUid uid, WoundComponent wound, ref ComponentHandleState args)
    {
        if (args.Current is not WoundComponentState state)
            return;

        wound.Parent = state.Parent;
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

    public IReadOnlyList<EntityUid> GetAllWoundEntities(EntityUid woundableId)
    {
        return _containers.GetContainer(woundableId, WoundContainerId).ContainedEntities;
    }

    public IReadOnlyList<WoundComponent> GetAllWoundComponents(EntityUid woundableId)
    {
        List<WoundComponent> components = new();
        foreach (var entityUid in GetAllWoundEntities(woundableId))
        {
            if (TryComp<WoundComponent>(entityUid, out var woundComp))
            {
                components.Add((woundComp));
            }
        }
        return components;
    }


    public bool ApplyTraumaToPart(EntityUid target, string traumaType, TraumaDamage traumaDamage)
    {
        // TODO wounds before merge turn into tryget
        if (GetValidWoundable(target, traumaType) is not {Target: var woundableId, Woundable: var woundable})
            return false;

        var modifiedDamage = ApplyTraumaModifiers(traumaType, woundable.TraumaResistance, traumaDamage.Damage);
        return TryChooseWound(traumaType, modifiedDamage, out var woundId) &&
               ApplyRawWoundDamage(woundableId, modifiedDamage, woundable) && AddWound(woundableId, woundId, woundable);
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
        if (!Resolve(woundableId, ref woundable, false) || _net.IsClient)
            return false;

        var woundId = Spawn(woundPrototypeId, woundableId.ToCoordinates());
        var wound = Comp<WoundComponent>(woundId);

        var wounds = _containers.EnsureContainer<Container>(woundableId, WoundContainerId);
        if (!wounds.Insert(woundId))
            return false;
        wound.Parent = woundableId;
        Dirty(wound);
        woundable.HealthCapDamage += wound.Severity * wound.HealthCapDamage;
        ApplyRawIntegrityDamage(woundableId, wound.IntegrityDamage, woundable);
        return true;
    }

    public bool ApplyRawWoundDamage(EntityUid woundableId, FixedPoint2 woundDamage, WoundableComponent? woundable = null)
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

    public bool AddWoundSeverity(EntityUid woundableId, EntityUid woundId, FixedPoint2 severityIncrease,
        WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        UpdateWoundSeverity(woundable, wound, wound.Severity + severityIncrease);
        return true;
    }

    public bool SetWoundSeverity(EntityUid woundableId, EntityUid woundId, FixedPoint2 severityAmount,
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

    private void UpdateWoundSeverity(WoundableComponent woundable, WoundComponent wound, FixedPoint2 newSeverity)
    {
        newSeverity = FixedPoint2.Clamp(newSeverity, FixedPoint2.Zero, 100);
        var severityDelta = newSeverity - wound.Severity;
        if (severityDelta == 0)
            return;
        var healthCapDamageDelta = severityDelta * wound.HealthCapDamage;
        woundable.HealthCapDamage += healthCapDamageDelta;
        wound.Severity = newSeverity;
    }

    public bool RemoveWound(EntityUid woundableId, EntityUid woundId,bool makeScar = false, WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false) ||
            !Resolve(woundId, ref wound, false))
            return false;

        _containers.RemoveEntity(woundableId, woundId);
        wound.Parent = EntityUid.Invalid;
        Dirty(wound);
        UpdateWoundSeverity(woundable, wound, FixedPoint2.Zero);

        if (makeScar && wound.ScarWound != null)
            AddWound(woundableId, wound.ScarWound, woundable);

        if (_net.IsServer)
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
