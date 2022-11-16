using System.Collections.ObjectModel;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class InjurySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    private readonly Dictionary<string, InjuryTable> _cachedInjuryTables = new();

    public override void Initialize()
    {
        CacheData(null);
        _prototypeManager.PrototypesReloaded += CacheData;
    }

    private void CacheData(PrototypesReloadedEventArgs? prototypesReloadedEventArgs)
    {
        _cachedInjuryTables.Clear();

        foreach (var traumaType in _prototypeManager.EnumeratePrototypes<TraumaTypePrototype>())
        {
            _cachedInjuryTables.Add(traumaType.ID, new InjuryTable(traumaType));
        }
    }

    public ReadOnlyDictionary<FixedPoint2, string> GetInjuryTable(string traumaType)
    {
        return new ReadOnlyDictionary<FixedPoint2, string>(_cachedInjuryTables[traumaType].Injuries);
    }

    public bool TryApplyWound(EntityUid target, TraumaSpecifier traumaSpec)
    {
        var success = false;
        foreach (var (traumaType, trauma) in traumaSpec.TraumaValues)
        {
            var validTarget = GetValidInjurable(target, traumaType);

            if (!validTarget.HasValue)
                return false;

            var injuryContainer = validTarget.Value.injurable;
            target = validTarget.Value.target;

            success |= AddInjury(target, injuryContainer, traumaType,
                PickInjury(traumaType,
                    ApplyTraumaModifiers(traumaType, injuryContainer.TraumaResistance, trauma.Damage)));

            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenTraumaType != null)
            {
                if (!_random.Prob(trauma.PenetrationChance.Float()))
                    continue;
            }

            validTarget = GetValidInjurable(target, type);

            if (!validTarget.HasValue)
                continue;

            //Apply penetrating wounds
            injuryContainer = validTarget.Value.injurable;
            target = validTarget.Value.target;
            success |= AddInjury(target, injuryContainer, type,
                PickInjury(type, ApplyTraumaModifiers(type, injuryContainer.TraumaResistance, trauma.Damage)));
        }

        return success;
    }

    public bool TryAddInjury(EntityUid target, string traumaType, Trauma trauma)
    {
        var checkedContainer = GetValidInjurable(target, traumaType);

        if (checkedContainer == null)
            return false;

        var injuryContainer = checkedContainer.Value.injurable;
        target = checkedContainer.Value.target;
        return AddInjury(target, injuryContainer, traumaType, PickInjury(traumaType, trauma.Damage));
    }

    private FixedPoint2 ApplyTraumaModifiers(string traumaType, TraumaModifierSet? modifiers, FixedPoint2 damage)
    {
        if (!modifiers.HasValue)
            return damage;

        if (modifiers.Value.TryGetCoefficientForTraumaType(traumaType, out var coeff))
            damage *= coeff;

        if (modifiers.Value.TryGetFlatReductionForTraumaType(traumaType, out var reduction))
            damage -= reduction;

        return damage;
    }

    private bool AddInjury(EntityUid target, InjurableComponent injuryContainer, string traumaType, Injury injury)
    {
        injuryContainer.Injuries ??= new List<Injury>();
        injuryContainer.Injuries.Add(injury);

        return true;
    }

    private (InjurableComponent injurable, EntityUid target)? GetValidInjurable(EntityUid target, string traumaType)
    {
        if (!TryComp<InjurableComponent>(target, out var injuryContainer))
            return null;

        if (injuryContainer.AllowedTraumaTypes?.Contains(traumaType) ?? false)
            return (injuryContainer, target);

        var childInjuryContainer = FindValidInjurableInAdjacent(target, traumaType);
        return childInjuryContainer;
    }

    private Injury PickInjury(string traumaType, FixedPoint2 trauma)
    {
        var nextLevel = 1f;
        var levelFloor = 0f;
        var injuryId = _cachedInjuryTables[traumaType].Injuries[FixedPoint2.Zero];

        foreach (var injuryData in _cachedInjuryTables[traumaType].Injuries)
        {
            if (injuryData.Key > trauma)
            {
                nextLevel = injuryData.Key.Float();
                break;
            }

            injuryId = injuryData.Value;
            levelFloor = injuryData.Key.Float();
        }

        return new Injury(injuryId, (trauma - levelFloor) / (nextLevel - trauma));
    }

    private (InjurableComponent Injurable, EntityUid Target)? FindValidInjurableInAdjacent(EntityUid target,
        string traumaType)
    {
        //search all the children in the body for a part that accepts the wound we want to apply
        //checks organs first, then recursively checks child parts and their organs.
        var foundContainer = FindValidInjurableInOrgans(target, traumaType) ??
                             FindValidInjurableInAdjacentParts(target, traumaType);
        return foundContainer;
    }

    private (InjurableComponent Injurable, EntityUid Target)? FindValidInjurableInAdjacentParts(EntityUid target,
        string traumaType)
    {
        foreach (var data in _bodySystem.GetBodyPartAdjacentPartsComponents<InjurableComponent>(target))
        {
            if (data.Component.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (data.Component, target);
        }

        return null;
    }

    private (InjurableComponent Injurable, EntityUid Target)? FindValidInjurableInOrgans(EntityUid target,
        string traumaType)
    {
        foreach (var data in _bodySystem.GetBodyPartOrganComponents<InjurableComponent>(target))
        {
            if (data.Comp.AllowedTraumaTypes?.Contains(traumaType) ?? false)
                return (data.Comp, target);
        }

        return null;
    }
}

public readonly struct InjuryTable
{
    //we do some defensive coding
    public readonly SortedDictionary<FixedPoint2, string> Injuries;

    public InjuryTable(TraumaTypePrototype traumaProto)
    {
        Injuries = traumaProto.WoundPool;
    }
}
