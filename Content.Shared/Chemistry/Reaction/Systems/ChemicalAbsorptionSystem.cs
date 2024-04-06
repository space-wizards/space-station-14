using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction.Components;
using Content.Shared.Chemistry.Reaction.Events;
using Content.Shared.Chemistry.Reaction.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reaction.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ChemicalAbsorptionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChemicalAbsorberComponent, ComponentInit>(OnAbsorberInit);
    }

    private void OnAbsorberInit(EntityUid uid, ChemicalAbsorberComponent component, ref ComponentInit args)
    {
        CacheAbsorptionOrder((uid, component));
    }

    private void CacheAbsorptionOrder(Entity<ChemicalAbsorberComponent> absorber)
    {
        absorber.Comp.CachedAbsorptionOrder.Clear();
        HashSet<AbsorptionPrototype> addedAbsorptions = new();
        SortedList<int, CachedAbsorptionData> absorptions = new();
        foreach (var absorptionGroupId in absorber.Comp.AbsorptionGroups)
        {
            var absorptionGroup = _protoManager.Index(absorptionGroupId);
            foreach (var absorptionId in absorptionGroup.Absorptions)
            {
                var absorption = _protoManager.Index(absorptionId);
                if (!addedAbsorptions.Add(absorption)) //prevent dupes
                    continue;
                absorptions.Add(absorption.Priority, GetCachedData(absorption));
            }
        }
        if (absorber.Comp.AdditionalAbsorptions != null)
        {
            foreach (var absorptionId in absorber.Comp.AdditionalAbsorptions)
            {
                var absorption = _protoManager.Index(absorptionId);
                if (!addedAbsorptions.Add(absorption)) //prevent dupes
                    continue;
                absorptions.Add(absorption.Priority, GetCachedData(absorption));
            }
        }
        absorber.Comp.CachedAbsorptionOrder = absorptions.Values.ToList();
        Dirty(absorber);
    }

    private CachedAbsorptionData GetCachedData(AbsorptionPrototype absorptionProto)
    {
        List<(ProtoId<ReagentPrototype>, FixedPoint2)> reagentAmounts = new();
        foreach (var (reagentProtoId, quantity) in absorptionProto.Reagents)
        {
            reagentAmounts.Add((new ProtoId<ReagentPrototype>(reagentProtoId), quantity));
        }

        List<(ProtoId<ReagentPrototype>, FixedPoint2)>? catalystAmounts = null;
        if (absorptionProto.Catalysts != null)
        {
            catalystAmounts = new();
            foreach (var (reagentProtoId, quantity) in absorptionProto.Catalysts)
            {
                catalystAmounts.Add((new ProtoId<ReagentPrototype>(reagentProtoId), quantity));
            }
        }
        return new CachedAbsorptionData(reagentAmounts,
            catalystAmounts, absorptionProto);
    }

    public bool TryGetCachedAbsorption(Entity<ChemicalAbsorberComponent> absorber,
        ProtoId<AbsorptionPrototype> absorptionProto, [NotNullWhen(true)] out CachedAbsorptionData? foundCachedData)
    {
        foundCachedData = null;
        foreach (var cachedData in absorber.Comp.CachedAbsorptionOrder)
        {
            if (absorptionProto.Id == cachedData.ProtoId)
            {
                foundCachedData = cachedData;
                return true;
            }
        }
        return false;
    }


    public void AbsorbChemicals(Entity<SolutionComponent> solutionEntity,
        Entity<ChemicalAbsorberComponent>? absorber = null)
    {
        Entity<ChemicalAbsorberComponent> foundAbsorber;
        if (absorber == null)
        {
            var parentEnt = _transformSystem.GetParentUid(solutionEntity);
            if (!TryComp<ChemicalAbsorberComponent>(parentEnt, out var absorberComponent))
                return;
            foundAbsorber = (parentEnt, absorberComponent);
        }
        else
        {
            foundAbsorber = absorber.Value;
        }

        foreach (var absorptionData in foundAbsorber.Comp.CachedAbsorptionOrder)
        {
            AbsorbChemical(foundAbsorber, solutionEntity, absorptionData);
        }
    }



    private void AbsorbChemical(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        CachedAbsorptionData cachedAbsorption)
    {
        var rate = GetAbsorptionRate(absorber, solutionEntity, cachedAbsorption);
        var absorption = _protoManager.Index(cachedAbsorption.ProtoId);
        if (rate <= 0)
            return;
        var solution = solutionEntity.Comp.Solution;
        foreach (var (reactantName, volume) in absorption.Reagents)
        {
                var amountToRemove = rate * volume;
                //TODO: this might run into issues with reagentData
                solution.RemoveReagent(reactantName, amountToRemove);
        }
        if (absorption.TransferHeat)
        {
            var thermalEnergy = solution.GetThermalEnergy(_protoManager);
            //TODO: actually apply the thermal energy to the absorber entity. Can't do that from shared...
            // Because for some fucking reason temperatureSystem is server only. Why! Temperature should be predicted!
        }
        if (absorption.Impact != null)
        {
            var posFound = _transformSystem.TryGetMapOrGridCoordinates(solutionEntity, out var gridPos);
            _adminLogger.Add(LogType.ChemicalReaction, absorption.Impact.Value,
                $"Chemical absorption {absorption.ID} occurred {rate} times on entity {ToPrettyString(solutionEntity)} " +
                $"at Pos:{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found]")}");
        }

        foreach (var solutionEffect in absorption.Effects)
        {
            solutionEffect.Target = absorber;
            solutionEffect.SolutionEntity = solutionEntity;
            solutionEffect.EntityManager = EntityManager;
            if (solutionEffect.CheckCondition())
                solutionEffect.TriggerEffect();
        }

        //TODO refactor this when reagentEffects get rewritten to not fucking hardcode organs
        //TODO: Also remove this once all ReagentEffects are converted to ReagentEvents
        var args = new ReagentEffectArgs(solutionEntity, null, solutionEntity.Comp.Solution,
            null,  rate, EntityManager, null, 1f);
        foreach (var effect in absorption.ReagentEffects)
        {
            if (!effect.ShouldApply(args))
                continue;

            if (effect.ShouldLog)
            {
                var posFound = _transformSystem.TryGetMapOrGridCoordinates(solutionEntity, out var gridPos);
                var entity = args.SolutionEntity;
                _adminLogger.Add(LogType.ReagentEffect, effect.LogImpact,
                    $"Absorption effect {effect.GetType().Name:effect} of absorption " +
                    $"{absorption.ID} applied on entity {ToPrettyString(entity):entity} at Pos:" +
                    $"{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found")}");
            }
            effect.Effect(args);
        }

        absorber.Comp.LastUpdate = _timing.CurTime;
        Dirty(absorber);

        _audioSystem.PlayPvs(absorption.Sound, solutionEntity);

        var ev = new ChemicalAbsorbedEvent(absorption, solutionEntity, absorber);
        RaiseLocalEvent(absorber, ref ev);
        RaiseLocalEvent(solutionEntity, ref ev);
    }

    public bool CanAbsorb(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        ProtoId<AbsorptionPrototype> absorptionId)
    {
        if (!TryGetCachedAbsorption(absorber, absorptionId, out var foundData))
            return false;
        return GetAbsorptionRate(absorber, solutionEntity, foundData.Value) > 0;
    }

    private FixedPoint2 GetAbsorptionRate(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        CachedAbsorptionData absorption
        )
    {
        //TODO: Move this over to a shared method in chemSystem after chem gets refactored to use ReactionRates
        //The logic for this will basically be the same as for reactions after reaction rates are implemented
        var solution = solutionEntity.Comp.Solution;
        if (solution.Temperature > absorption.MaxTemp
            || solution.Temperature < absorption.MinTemp)
            return FixedPoint2.Zero;

        var duration =_timing.CurTime.TotalSeconds - absorber.Comp.LastUpdate.TotalSeconds;
        //if any of these are negative something has fucked up
        DebugTools.Assert(duration < 0 || absorber.Comp.RateMultiplier < 0 || absorption.Rate < 0);
        var reactionRate = duration / (absorption.Rate * absorber.Comp.RateMultiplier);
        if (reactionRate == 0 || absorption.Quantized && reactionRate < 1)
            return FixedPoint2.Zero;

        if (absorption.RequiredCatalysts != null)
        {
            foreach (var (reagentName, requiredVolume) in absorption.RequiredCatalysts)
            {
                var reactantQuantity = solution.GetTotalPrototypeQuantity(reagentName);
                if (reactantQuantity == FixedPoint2.Zero || absorption.Quantized && reactantQuantity < requiredVolume)
                    return FixedPoint2.Zero;
            }
        }

        foreach (var (reagentName, requiredVolume) in absorption.RequiredReagents)
        {
            var reactantQuantity = solution.GetTotalPrototypeQuantity(reagentName);

            if (reactantQuantity <= FixedPoint2.Zero)
                return FixedPoint2.Zero;

            var unitRate = reactantQuantity / requiredVolume;
            if (absorption.Quantized)
                unitRate = MathF.Floor(unitRate.Float());

            if (unitRate < reactionRate)
            {
                reactionRate = unitRate;
            }
        }

        var ev = new ChemicalAbsorbAttemptEvent(_protoManager.Index(absorption.ProtoId), solutionEntity, absorber);
        RaiseLocalEvent(absorber, ref ev);
        RaiseLocalEvent(solutionEntity, ref ev);
        if (ev.Canceled)
            return FixedPoint2.Zero;

        //TODO: condition check

        return reactionRate;
    }

}
