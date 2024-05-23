using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reaction.Systems;

public sealed class ChemicalRateReactionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public float GetReactionRate<T>(
        Entity<SolutionComponent> targetSolution,
        T reaction,
        TimeSpan lastUpdate,
        float multiplier = 1.0f,
        bool ignoreTimeScaling = false) where T: struct, IReactionData
    {
        //TODO: Move this over to a shared method in chemSystem after chem gets refactored to use ReactionRates
        //The logic for this will basically be the same as for reactions after reaction rates are implemented
        var solution = targetSolution.Comp.Solution;
        //(when not forced)  Prevent running reactions multiple times in the same tickif we end up calling this
        //function multiple times This prevents the burning orphanage scenario
        //(still don't mess with solutions from within solution effects)
        if (solution.Temperature > reaction.MaxTemp
            || solution.Temperature < reaction.MinTemp
            //(when not forced)  Prevent running reactions multiple times in the same tickif we end up calling this
            //function multiple times This prevents the burning orphanage scenario
            //(still don't mess with solutions from within solution effects)
            || !ignoreTimeScaling && lastUpdate == _timing.CurTime
           )
            return 0;

        var unitReactions = AdjustReactionRateByTime(reaction.Rate,
            lastUpdate,
            multiplier,
            ignoreTimeScaling);
        if (unitReactions == 0 || reaction.Quantized && unitReactions < 1)
            return 0;
        if (unitReactions > 1)
            unitReactions = 1;

        if (reaction.Catalysts != null)
        {
            foreach (var (reagentName, requiredVolume) in reaction.Catalysts)
            {
                var reactantQuantity = solution.GetTotalPrototypeQuantity(reagentName) * multiplier;
                if (reactantQuantity == FixedPoint2.Zero || reaction.Quantized && reactantQuantity < requiredVolume)
                    return 0;
                //Limit reaction rate by catalysts, technically catalysts would allow you to accelerate a reaction rate past
                //it's normal rate but that's functionality that someone else can add later. For now, we are assuming that
                //the rate specified on the reaction/absorption is the maximum catalyzed rate if catalysts are specified.
                UpdateReactionRateFromReactants(ref unitReactions,
                    reactantQuantity,
                    requiredVolume,
                    lastUpdate,
                    multiplier,
                    reaction.Quantized,
                    ignoreTimeScaling);
            }
        }

        foreach (var (reagentName, requiredVolume) in reaction.Reactants)
        {
            var reactantQuantity = solution.GetTotalPrototypeQuantity(reagentName);
            if (reactantQuantity <= FixedPoint2.Zero)
                return 0;
            UpdateReactionRateFromReactants(ref unitReactions,
                reactantQuantity,
                requiredVolume,
                lastUpdate,
                multiplier,
                reaction.Quantized,
                ignoreTimeScaling);
        }
        return unitReactions;
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
        ref float reactionRate,
        FixedPoint2 quantity,
        FixedPoint2 requiredVolume,
        TimeSpan lastUpdate,
        float multiplier,
        bool quantized,
        bool ignoreTimeScaling)
    {
        var unitReactions = AdjustReactionRateByTime(quantity.Float() / requiredVolume.Float(),
            lastUpdate,
            multiplier,
            ignoreTimeScaling);
        if (quantized)
            unitReactions = MathF.Floor(unitReactions);

        if (unitReactions < reactionRate)
        {
            reactionRate = unitReactions;
        }
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
    private float AdjustReactionRateByTime(float reactionRate,
        TimeSpan lastUpdate,
        float multiplier,
        bool ignoreTimeScaling)
    {
        if (ignoreTimeScaling)
            return reactionRate * multiplier;
        var duration =(float)(_timing.CurTime.TotalSeconds - lastUpdate.TotalSeconds);
        //if any of these are negative something has fucked up
        DebugTools.Assert(duration < 0 || multiplier < 0 || reactionRate < 0);
        if (reactionRate == 0) //let's not throw an exception shall we
            return 0;
        return duration / reactionRate  * multiplier;
    }

    public void RunReaction(
        EntityUid target,
        Entity<SolutionComponent> targetSolution,
        RateReaction reaction,
        TimeSpan lastUpdate,
        float multiplier = 1.0f,
        bool ignoreTimeScaling = false)
    {
        var unitAbsorptions = GetReactionRate(targetSolution, reaction, lastUpdate, multiplier, ignoreTimeScaling);
        if (unitAbsorptions <= 0)
            return;

        foreach (var (reactantName, volume) in reaction.Reactants)
        {
            var amountToRemove = unitAbsorptions * volume;
            targetSolution.Comp.Solution.RemoveReagent(reactantName, amountToRemove);
        }
        foreach (var (reactantName, volume) in reaction.Products)
        {
            var amountToAdd = unitAbsorptions * volume;
            targetSolution.Comp.Solution.AddReagent(reactantName, amountToAdd);
        }

        if (reaction.TransferHeat)
        {
            var thermalEnergy = targetSolution.Comp.Solution.GetThermalEnergy(_protoManager);
            //TODO: actually apply the thermal energy to the reaction entity. Can't do that from shared...
            // Because for some fucking reason temperatureSystem is server only. Why! Temperature should be predicted!
        }
        if (reaction.Impact != null)
        {
            var posFound = _transformSystem.TryGetMapOrGridCoordinates(targetSolution, out var gridPos);
            _adminLogger.Add(LogType.ChemicalReaction,
                reaction.Impact.Value,
                $"Chemical absorption {reaction.ProtoId} occurred {unitAbsorptions} times on entity {ToPrettyString(targetSolution)} " +
                $"at Pos:{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found]")}");
        }

        if (reaction.Effects != null)
        {
            foreach (var solutionEffect in reaction.Effects)
            {
                solutionEffect.RaiseEvent(EntityManager, target, targetSolution);
            }
        }

        if (reaction.ReagentEffects != null)
        {
            //TODO refactor this when reagentEffects get rewritten to not fucking hardcode organs
            //TODO: Also remove this once all ReagentEffects are converted to ReagentEvents
            var args = new ReagentEffectArgs(targetSolution,
                null,
                targetSolution.Comp.Solution,
                null,
                unitAbsorptions,
                EntityManager,
                null,
                1f);
            foreach (var effect in reaction.ReagentEffects)
            {
                if (!effect.ShouldApply(args))
                    continue;

                if (effect.ShouldLog)
                {
                    var posFound = _transformSystem.TryGetMapOrGridCoordinates(targetSolution, out var gridPos);
                    var entity = args.SolutionEntity;
                    _adminLogger.Add(LogType.ReagentEffect,
                        effect.LogImpact,
                        $"Absorption effect {effect.GetType().Name:effect} of absorption " +
                        $"{reaction.ProtoId} applied on entity {ToPrettyString(entity):entity} at Pos:" +
                        $"{(posFound ? $"{gridPos:coordinates}" : "[Grid or Map not Found")}");
                }

                effect.Effect(args);
            }
        }
        _solutionSystem.UpdateChemicals(targetSolution, true, false);
        _audioSystem.PlayPvs(reaction.Sound, targetSolution);
    }
}
