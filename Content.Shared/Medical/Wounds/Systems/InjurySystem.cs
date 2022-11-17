using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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

        SubscribeLocalEvent<BodyComponent, AttackedEvent>(OnBodyAttacked);
    }

    private void OnBodyAttacked(EntityUid uid, BodyComponent component, AttackedEvent args)
    {
        if (!TryComp(args.Used, out TraumaInflictorComponent? inflictor))
            return;

        foreach (var child in _bodySystem.GetBodyChildren(uid, component))
        {
            TryApplyWound(child.Id, inflictor.Trauma);
        }
    }

    private void CacheData(PrototypesReloadedEventArgs? prototypesReloadedEventArgs)
    {
        _cachedInjuryTables.Clear();

        foreach (var traumaType in _prototypeManager.EnumeratePrototypes<TraumaTypePrototype>())
        {
            _cachedInjuryTables.Add(traumaType.ID, new InjuryTable(traumaType));
        }
    }

    public IReadOnlyDictionary<FixedPoint2, string> GetInjuryTable(string traumaType)
    {
        return _cachedInjuryTables[traumaType].Injuries;
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

            var modifiers = ApplyTraumaModifiers(traumaType, injuryContainer.TraumaResistance, trauma.Damage);
            if (TryPickInjury(traumaType, modifiers, out var injury))
            {
                success |= AddInjury(target, injuryContainer, traumaType, injury);
            }

            var type = trauma.PenTraumaType ?? traumaType;

            if (trauma.PenetrationChance > 0 && !_random.Prob(trauma.PenetrationChance.Float()))
                continue;

            validTarget = GetValidInjurable(target, type);

            if (!validTarget.HasValue)
                continue;

            //Apply penetrating wounds
            injuryContainer = validTarget.Value.injurable;
            target = validTarget.Value.target;

            modifiers = ApplyTraumaModifiers(type, injuryContainer.TraumaResistance, trauma.Damage);
            if (TryPickInjury(type, modifiers, out injury))
            {
                success |= AddInjury(target, injuryContainer, type, injury);
            }
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

        return TryPickInjury(traumaType, trauma.Damage, out var injury) &&
               AddInjury(target, injuryContainer, traumaType, injury);
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

    private bool TryPickInjury(string traumaType, FixedPoint2 trauma, out Injury injury)
    {
        var nextLevel = 1f;
        var levelFloor = 0f;
        // TODO what if 0 is not in the dict
        if (!_cachedInjuryTables[traumaType].Injuries.TryFirstOrNull(out var cachedInjury))
        {
            injury = default;
            return false;
        }

        var injuryId = cachedInjury.Value.Value;

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

        injury = new Injury(injuryId, (trauma - levelFloor) / (nextLevel - trauma));
        return true;
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
