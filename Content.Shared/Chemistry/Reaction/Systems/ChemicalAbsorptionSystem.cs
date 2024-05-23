using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
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
    [Dependency] private readonly ChemicalRateReactionSystem _rateReactionSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChemicalAbsorberComponent, ComponentInit>(OnAbsorberInit);
        SubscribeLocalEvent<ChemicalAbsorberComponent, MapInitEvent>(OnAbsorberMapInit);
    }

    private void OnAbsorberInit(EntityUid uid, ChemicalAbsorberComponent component, ref ComponentInit args)
    {
        UpdateAbsorptionCache((uid, component));
        component.LastUpdate = _timing.CurTime;
    }

    private void OnAbsorberMapInit(EntityUid uid, ChemicalAbsorberComponent component, ref MapInitEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solMan))
        {
            Log.Error($"{ToPrettyString(uid)} has an absorberComponent but not solutionManager!");
            return;
        }
        AbsorbChemicals((uid, component, solMan), false);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ChemicalAbsorberComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var absorber, out var solMan))
        {
            if (_timing.CurTime < absorber.LastUpdate + absorber.UpdateRate)
                continue;
            AbsorbChemicals((uid,absorber, solMan), false);
        }
    }


    private void UpdateAbsorptionCache(Entity<ChemicalAbsorberComponent> absorber)
    {
        absorber.Comp.CachedAbsorptionOrder.Clear();
        HashSet<AbsorptionPrototype> addedAbsorptions = new();
        SortedList<int, AbsorptionReaction> absorptions = new();
        foreach (var (absorptionGroupId, multiplier) in absorber.Comp.AbsorptionGroups)
        {
            var absorptionGroup = _protoManager.Index(absorptionGroupId);
            foreach (var absorptionId in absorptionGroup.Absorptions)
            {
                var absorption = _protoManager.Index(absorptionId);
                if (!addedAbsorptions.Add(absorption)) //prevent dupes
                    continue;
                absorptions.Add(absorption.Priority, absorption.GetData(multiplier));
            }
        }
        if (absorber.Comp.AdditionalAbsorptions != null)
        {
            foreach (var (absorptionId, multiplier) in absorber.Comp.AdditionalAbsorptions)
            {
                var absorption = _protoManager.Index(absorptionId);
                if (!addedAbsorptions.Add(absorption)) //prevent dupes
                    continue;
                absorptions.Add(absorption.Priority, absorption.GetData(multiplier));
            }
        }
        absorber.Comp.CachedAbsorptionOrder = absorptions.Values.ToList();
        Dirty(absorber);
    }

    public bool TryGetCachedAbsorption(Entity<ChemicalAbsorberComponent> absorber,
        ProtoId<AbsorptionPrototype> absorptionProto,
        [NotNullWhen(true)] out AbsorptionReaction? foundCachedData)
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

        AbsorbChemicals(solutionEntity, foundAbsorber, false);
    }

    private void AbsorbChemicals(Entity<ChemicalAbsorberComponent, SolutionContainerManagerComponent> absorber, bool ignoreTimeScaling)
    {
        foreach (var solutionName in absorber.Comp1.LinkedSolutions)
        {
            if (!_solutionSystem.TryGetSolution((absorber, absorber.Comp2), solutionName, out var solutionEnt))
            {
                Log.Error($"Could not find solution with name: {solutionName} on {ToPrettyString(absorber)}");
                return;
            }
            AbsorbChemicals(solutionEnt.Value, (absorber, absorber.Comp1), ignoreTimeScaling);
        }
    }

    private void AbsorbChemicals(Entity<SolutionComponent> solutionEntity, Entity<ChemicalAbsorberComponent> absorber,
        bool ignoreTimeScaling)
    {
        foreach (var absorptionData in absorber.Comp.CachedAbsorptionOrder)
        {
            AbsorbChemical(absorber, solutionEntity, absorptionData, ignoreTimeScaling);
        }
    }

    private void AbsorbChemical(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        AbsorptionReaction cachedAbsorption,
        bool ignoreTimeScaling)
    {
        var unitAbsorptions = GetReactionRate(absorber, solutionEntity, cachedAbsorption, ignoreTimeScaling);
        if (unitAbsorptions <= 0)
            return;
        var solution = solutionEntity.Comp.Solution;

        var targetSolutionUpdated = false;
        Entity<SolutionComponent>? targetSolution = null;

        if (cachedAbsorption.CanTransfer && TryGetTargetSolution(absorber, out targetSolution))
        {
            foreach (var (reactantName, volume) in cachedAbsorption.Reactants)
            {
                var amountToRemove = unitAbsorptions * volume;
                //TODO: this might run into issues with reagentData
                targetSolution.Value.Comp.Solution.AddReagent(reactantName,solution.RemoveReagent(reactantName, amountToRemove));
                targetSolutionUpdated = true;
            }
        }
        else
        {
            foreach (var (reactantName, volume) in cachedAbsorption.Reactants)
            {
                var amountToRemove = unitAbsorptions * volume;
                //TODO: this might run into issues with reagentData
                solution.RemoveReagent(reactantName, amountToRemove);
            }
        }

        if (cachedAbsorption.TransferHeat)
        {
            var thermalEnergy = solution.GetThermalEnergy(_protoManager);
            //TODO: actually apply the thermal energy to the absorber entity. Can't do that from shared...
            // Because for some fucking reason temperatureSystem is server only. Why! Temperature should be predicted!
        }
        if (cachedAbsorption.Impact != null)
        {
            var posFound = _transformSystem.TryGetMapOrGridCoordinates(solutionEntity, out var gridPos);
            _adminLogger.Add(LogType.ChemicalReaction,
                cachedAbsorption.Impact.Value,
                $"Chemical absorption {cachedAbsorption.ProtoId} occurred {unitAbsorptions} times on entity {ToPrettyString(solutionEntity)} " +
                $"at Pos:{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found]")}");
        }

        if (cachedAbsorption.Effects != null)
        {
            foreach (var solutionEffect in cachedAbsorption.Effects)
            {
                solutionEffect.RaiseEvent(EntityManager, absorber, solutionEntity);
            }
        }

        if (cachedAbsorption.ReagentEffects != null)
        {
            //TODO refactor this when reagentEffects get rewritten to not fucking hardcode organs
            //TODO: Also remove this once all ReagentEffects are converted to ReagentEvents
            var args = new ReagentEffectArgs(solutionEntity,
                null,
                solutionEntity.Comp.Solution,
                null,
                unitAbsorptions,
                EntityManager,
                null,
                1f);
            foreach (var effect in cachedAbsorption.ReagentEffects)
            {
                if (!effect.ShouldApply(args))
                    continue;

                if (effect.ShouldLog)
                {
                    var posFound = _transformSystem.TryGetMapOrGridCoordinates(solutionEntity, out var gridPos);
                    var entity = args.SolutionEntity;
                    _adminLogger.Add(LogType.ReagentEffect,
                        effect.LogImpact,
                        $"Absorption effect {effect.GetType().Name:effect} of absorption " +
                        $"{cachedAbsorption.ProtoId} applied on entity {ToPrettyString(entity):entity} at Pos:" +
                        $"{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found")}");
                }

                effect.Effect(args);
            }
        }
        if (targetSolutionUpdated) // Dirty/Update the solutions, do this last so absorptions resolve before reactions
            _solutionSystem.UpdateChemicals(targetSolution!.Value, true, false);
        _solutionSystem.UpdateChemicals(solutionEntity, true, false);
        _audioSystem.PlayPvs(cachedAbsorption.Sound, solutionEntity);
    }

    public bool CanAbsorb(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        ProtoId<AbsorptionPrototype> absorptionId)
    {
        if (!TryGetCachedAbsorption(absorber, absorptionId, out var foundData))
            return false;
        return GetReactionRate(absorber, solutionEntity, foundData.Value, false) > 0;
    }

    private float GetReactionRate(
        Entity<ChemicalAbsorberComponent> absorber,
        Entity<SolutionComponent> solutionEntity,
        AbsorptionReaction absorption,
        bool ignoreTimeScaling)
    {
        var rate = _rateReactionSystem.GetReactionRate(solutionEntity,
            absorption,
            absorber.Comp.LastUpdate,
            absorber.Comp.GlobalRateMultiplier,
            ignoreTimeScaling);
        if (rate == 0)
            return 0;

        if (absorption.Conditions == null)
            return rate;
        foreach (var condition in absorption.Conditions)
        {
            var ev = condition.RaiseEvent(EntityManager, absorber, solutionEntity);
            if (!ev.Valid)
                return 0;
        }
        return rate;
    }

    /// <summary>
    /// Adjusts the reaction rate calculation by the amount of time that has passed since the last update
    /// This makes sure that reaction rates are always consistent and don't change based on the number of times you
    /// call the update function
    /// </summary>
    /// <param name="reactionRate"></param>
    /// <param name="lastUpdate"></param>
    /// <param name="multiplier"></param>
    /// <param name="ignoreTimeScaling"></param>
    /// <returns></returns>
    private FixedPoint2 AdjustReactionRateByTime(FixedPoint2 reactionRate, TimeSpan lastUpdate, FixedPoint2 multiplier,
        bool ignoreTimeScaling)
    {
        if (ignoreTimeScaling)
            return reactionRate * multiplier;
        var duration =_timing.CurTime.TotalSeconds - lastUpdate.TotalSeconds;
        //if any of these are negative something has fucked up
        DebugTools.Assert(duration < 0 || multiplier < 0 || reactionRate < 0);
        if (reactionRate == 0) //let's not throw an exception shall we
            return 0;
        return duration / reactionRate  * multiplier;
    }


    /// <summary>
    /// Updates the reaction rate if our new calculated rate is lower than the existing one
    /// </summary>
    /// <param name="reactionRate"></param>
    /// <param name="quantity"></param>
    /// <param name="requiredVolume"></param>
    /// <param name="lastUpdate"></param>
    /// <param name="multiplier"></param>
    /// <param name="quantized"></param>
    /// <param name="ignoreTimeScaling"></param>
    private void UpdateReactionRateFromReactants(
        ref FixedPoint2 reactionRate,
        FixedPoint2 quantity,
        FixedPoint2 requiredVolume,
        TimeSpan lastUpdate,
        FixedPoint2 multiplier,
        bool quantized,
        bool ignoreTimeScaling)
    {
        var unitReactions = AdjustReactionRateByTime(quantity / requiredVolume, lastUpdate,
            multiplier, ignoreTimeScaling);
        if (quantized)
            unitReactions = MathF.Floor(unitReactions.Float());

        if (unitReactions < reactionRate)
        {
            reactionRate = unitReactions;
        }
    }

    public bool TryGetTargetSolution(Entity<ChemicalAbsorberComponent> absorber,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;
        if (absorber.Comp.TransferTargetEntity == null)
            return false;
        if (_solutionSystem.TryGetSolution((absorber.Comp.TransferTargetEntity.Value, null),
                absorber.Comp.TransferTargetSolutionId, out solution))
            return false;
        return solution != null;

    }

}
