using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.Reaction;

public abstract partial class SharedChemicalReactionSystem
{
    /// <summary>
    /// Handles reactions within a solution until no more reactions may occur.
    /// </summary>
    /// <param name="uid">The entity to handle the reactions within.</param>
    /// <param name="solution">The solution to process the reactions within.</param>
    /// <param name="curTime">The time at which to handle the reactions.</param>
    /// <param name="reactions">The set of reactions that may be capable of occurring within the target solution.</param>
    /// <param name="ongoing">The set of reactions that are currently ongoing in the target solution mapped to their states.</param>
    /// <param name="allReactions">The set of all reactions currently ongoing in the target entity indexed by solution.</param>
    /// <param name="mixer">The state of some entity being used to mix the reacting solution.</param>
    protected void HandleReactions(
        EntityUid uid, Solution solution, TimeSpan curTime,
        SortedSet<ReactionSpecification> reactions, Dictionary<ReactionSpecification, ReactionData>? ongoing,
        ReactingComponent? allReactions = null, ReactionMixerComponent? mixer = null
    ) {
        var newGroup = ongoing == null;
        var i = MaxReactionIterations;
        while(HandleAnyReaction(uid, solution, curTime, reactions, ref ongoing, mixer))
        {
            if (--i <= 0)
            {
                Logger.Error($"{nameof(Solution)} {uid} could not finish reacting in under {MaxReactionIterations} loops.");
                break;
            }
        }

        var overflow = solution.TotalVolume - solution.MaxVolume;
        if (overflow > 0)
        {   // TODO: Handle spilling overflow from reactions.
            solution.RemoveSolution(overflow);
        }

        if (ongoing == null)
            return;

        // At this point ongoing includes all of the reactions that were occuring as of the start of this update and those that occurred during this update.
        var toStop = new List<ReactionSpecification>();
        foreach(var (reaction, data) in ongoing)
        {
            if (data.State == ReactionState.Stopping)
            {
                toStop.Add(reaction);
                OnReactionStop(reaction, uid, solution, curTime, data);
            }
        }

        foreach(var reaction in toStop)
        {
            ongoing.Remove(reaction);
        }

        switch((newGroup, ongoing.Count > 0))
        {
            case (true, true): // Reactions have begun in the target solution that did not resolve by the end of processing, add a reacting component to process them.
                allReactions ??= EnsureComp<ReactingComponent>(uid);
                allReactions.ReactionGroups.Add(solution, ongoing);
                break;
            case (false, false): // All reactions in the target solution have terminated. Remove the reaction group and, if necessary, remove the reacting component.
                allReactions!.ReactionGroups.Remove(solution);
                if (allReactions!.ReactionGroups.Count <= 0)
                    QueueLocalEvent(new AllReactionsStoppedMessage(uid, allReactions));
                break;
        }
    }

    /// <summary>
    /// Checks whether any reaction in a set is capable of occuring within a target container.
    /// If at least one can, that reaction is handled.
    /// If the reaction was not already occurring it is added to the set of ongoing reactions.
    /// If the reaction produced any reagents the products are merged with the target solution.
    /// Any reactions that could not occur within the target container up to the final one are removed from the set of reactions.
    /// Any reactions associated with produced reagents are added to the set of reactions.
    /// </summary>
    /// <param name="uid">The entity within which the reactions should occur.</param>
    /// <param name="solution">The solution within which the reactions should occur.</param>
    /// <param name="curTime">The time at which the reactions are being processed.</param>
    /// <param name="reactions">The set of reactions to handle.</param>
    /// <param name="ongoing">Any reactions that are already ongoing in the target solution mapped to their state.</param>
    /// <param name="mixer">An entity being used to stir the reacting solution if any exist.</param>
    private bool HandleAnyReaction(
        EntityUid uid, Solution solution, TimeSpan curTime,
        SortedSet<ReactionSpecification> reactions, ref Dictionary<ReactionSpecification, ReactionData>? ongoing,
        ReactionMixerComponent? mixer = null
    ) {
        var result = false;
        Solution? products = null;
        List<ReactionSpecification> toRemove = new();
        foreach(var reaction in reactions)
        {
            ReactionData? data = null;
            ongoing?.TryGetValue(reaction, out data);
            if(!HandleReaction(reaction, uid, solution, curTime, ref data, out products, mixer))
            {
                toRemove.Add(reaction);
                continue;
            }

            switch(data!.State)
            {   // Both starting reactions and progressing reactions probably change the state of the solution.
                case ReactionState.Starting:
                    ongoing ??= new();
                    ongoing.Add(reaction, data);
                    goto case ReactionState.Running;
                case ReactionState.Running:
                    result = true;
                    break;
            }
        }

        reactions.ExceptWith(toRemove);

        if (products != null && products.TotalVolume > FixedPoint2.Zero)
        {
            foreach(var product in products)
            {
                if (_reactions.TryGetValue(product.ReagentId, out var productReactions))
                    reactions.UnionWith(productReactions);
            }
            solution.AddSolution(products);
        }

        return result;
    }

    /// <summary>
    /// Checks whether a single reaction is capable of being processed and, if it can, handles its effects.
    /// </summary>
    /// <param name="reaction">The reaction to process.</param>
    /// <param name="uid">The entity within which the reaction should occur.</param>
    /// <param name="solution">The solution within which the reaction should occur.</param>
    /// <param name="curTime">The time at which the reactions are being processed.</param>
    /// <param name="data">A wrapper for the state of the reaction within the target solution.</param>
    /// <param name="products">The reagents produced by a sucessful reaction.</param>
    /// <param name="mixer">An entity being used to stir the reacting solution if any exist.</param>
    private bool HandleReaction(
        ReactionSpecification reaction,
        EntityUid uid, Solution solution, TimeSpan curTime,
        ref ReactionData? data, out Solution? products,
        ReactionMixerComponent? mixer = null
    ) {
        products = null;
        if(!CanReact(reaction, uid, solution, curTime, data, out var amount, mixer))
        {
            if (data != null)
                data.State = ReactionState.Stopping;
            return false;
        }

        if (data == null)
            OnReactionStart(reaction, uid, solution, curTime, out data);
        else if(amount > 0f)
            OnReactionStep(reaction, uid, solution, curTime, amount, data, out products);
        else
            data.State = ReactionState.Paused;

        return true;
    }

    /// <summary>
    /// Checks whether a chemical reaction is capable of occuring within a solution.
    /// </summary>
    /// <param name="reaction">The reaction to check for viability.</param>
    /// <param name="uid">The entity that the reaction wants to occur in.</param>
    /// <param name="solution">The <see cref="Solution"/> that the reaction wants to occur in.</param>
    /// <param name="curTime">The time at which the reaction wants to occur.</param>
    /// <param name="data">The state of the reaction ongoing within the solution.</param>
    /// <param name="amount">The amount of the reaction which can occur within the solution. May be <= <see cref="FixedPoint2.Zero"/>.</param>
    /// <param name="mixer">The state of some device being used to mix the solution if any such entity exists.</param>
    protected bool CanReact(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, ReactionData? data, out float amount, ReactionMixerComponent? mixer = null)
    {
        amount = float.PositiveInfinity;
        if (reaction.MinimumTemperature > float.NegativeInfinity
        ||  reaction.MaximumTemperature < float.PositiveInfinity)
        {
            if (solution.Temperature < reaction.MinimumTemperature)
            {
                amount = 0f;
                return false;
            } else if(solution.Temperature > reaction.MaximumTemperature)
            {
                amount = 0f;
                return false;
            }

            if (reaction.HeatDelta != 0 || reaction.HeatCapacityDelta != 0)
            {
                // Reaction units required to reach a specific temperature T:  x = (TC0 - T0C0) / (dU - TdC)
                // It's the inverse of the function for the temperature of the solution after x units of reaction: T = (T0C0 + dUx) / (C0 + dCx)
                var C0 = solution.HeatCapacity;
                var T0C0 = solution.Temperature * C0;
                var dU = reaction.HeatDelta;
                var dC = reaction.HeatCapacityDelta;
                float divisor;
                float limit;

                divisor = dU - reaction.MinimumTemperature * dC;
                if (divisor != 0)
                {
                    limit = (reaction.MinimumTemperature * C0 - T0C0) / divisor;
                    if (limit >= 0f && limit < amount)
                    {   // If the limit is negative then the reaction will bring the solution further away from the thermal limit as it progresses.
                        amount = limit;
                        if (amount <= 0f)
                            return false;
                    }
                }

                divisor = dU - reaction.MaximumTemperature * dC;
                if (divisor != 0)
                {
                    limit = (reaction.MaximumTemperature * C0 - T0C0) / divisor;
                    if (limit >= 0f && limit < amount)
                    {   // If the limit is negative then the reaction will bring the solution further away from the thermal limit as it progresses.
                        amount = limit;
                        if (amount <= 0f)
                            return false;
                    }
                }
            }
        }

        if(!reaction.CanOverflow && reaction.VolumeDelta > 0f)
        {
            var limit = (float)(solution.AvailableVolume / reaction.VolumeDelta);
            if (limit < amount)
            {
                amount = limit;
                if (amount <= 0f)
                    return false;
            }
        }

        // This is so continuous mixing can work.
        List<string> currentlyMixingTypes = mixer != null ? new(mixer.ReactionTypes) : new();
        var attempt = new ReactionAttemptEvent(reaction, solution, uid, currentlyMixingTypes);
        RaiseLocalEvent(uid, attempt, false);
        if (attempt.Cancelled)
        {
            amount = 0f;
            return false;
        }

        if (reaction.MixingCategories != null
        && (data == null || reaction.NeedsContinuousMixing)
        && (!currentlyMixingTypes.Any() || reaction.MixingCategories.Except(currentlyMixingTypes).Any()))
        {
            amount = 0f;
            return false;
        }

        foreach (var reactantData in reaction.Reactants)
        {
            var reactantName = reactantData.Id;
            var reactantCoefficient = reactantData.Amount;

            if (!solution.ContainsReagent(reactantName, out var reactantQuantity))
                return false;

            if (reactantData.Catalyst)
            {
                // catalyst is not consumed, so will not limit the reaction. But it still needs to be present, and
                // for quantized reactions we need to have a minimum amount

                if (reactantQuantity == FixedPoint2.Zero || reaction.Quantized && reactantQuantity < reactantCoefficient)
                    return false;

                continue;
            }

            var unitReactions = (float)(reactantQuantity / reactantCoefficient);

            if (unitReactions < amount)
                amount = unitReactions;
        }

        if(!float.IsFinite(reaction.ReactionRate))
        {   // Infinitely fast reactions are considered as completing in a single update even with a timestep of size 0.
            if (reaction.Quantized)
                amount = (int) amount;
            return amount > FixedPoint2.Epsilon;
        }

        if(amount < (reaction.Quantized ? 1 : FixedPoint2.Epsilon))
            return false;

        TimeSpan frameTime = data != null ? (curTime - data.LastTime) : TimeSpan.Zero;
        var rateLimit = reaction.ReactionRate * (float)frameTime.TotalSeconds;
        if (rateLimit < amount)
            amount = rateLimit;
        return true; // We technically _can_ react, we are just limited by the available time.
    }

    /// <summary>
    /// Handles any effects a reaction has when the reaction begins to react.
    /// </summary>
    /// <param name="reaction">The reaction that beginning to react.</param>
    /// <param name="uid">The entity that the reaction is beginning within.</param>
    /// <param name="solution">The <see cref="Solution"/> that the reaction is beginning within.</param>
    /// <param name="curTime">The reaction that continuing to react.</param>
    /// <param name="data">A wrapper for the state of the reaction within the solution created at the start of the reaction.</param>
    protected virtual void OnReactionStart(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, out ReactionData data)
    {
        data = new(curTime);
        data.State = ReactionState.Starting;
        if (reaction.StartEffects == null)
            return;

        var args = new ReagentEffectArgs(uid, null, solution, null, reaction, FixedPoint2.Zero, TimeSpan.Zero, EntityManager, ReactionMethod.None, 1);
        RaiseLocalEvent(uid, ref args); // TODO: Make this handle reagent effect args.

        foreach (var effect in reaction.StartEffects)
        {
            if (effect.ShouldApply(ref args, _random))
                continue;

            if (effect.ShouldLog)
            {
                _adminLogger.Add(
                    LogType.ReagentEffect,
                    effect.LogImpact,
                    $"Reaction effect {effect.GetType().Name:effect} of reaction ${reaction.ID:reaction} applied on entity {ToPrettyString(uid):entity} at {Transform(uid).Coordinates:coordinates}"
                );
            }

            effect.Effect(ref args);
        }
    }

    /// <summary>
    /// Handles a reaction occuring within a solution.
    /// </summary>
    /// <param name="reaction">The reaction that continuing to react.</param>
    /// <param name="uid">The entity that the reaction is occuring within.</param>
    /// <param name="solution">The <see cref="Solution"/> that the reaction is occuring within.</param>
    /// <param name="curTime">The time at which this reaction step is occuring.</param>
    /// <param name="amount">The size of the reaction step which is occuring.</param>
    /// <param name="data">The state of the reaction ongoing in the solution.</param>
    /// <param name="products">The set of reagents produced by this reaction during its reaction.</param>
    protected virtual bool OnReactionStep(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, float amount, ReactionData data, out Solution? products)
    {
        data.State = ReactionState.Running;
        TimeSpan frameTime = curTime - data.LastTime;
        data.LastTime = curTime;
        ref var stepAmt = ref data.LastStep;
        stepAmt = data.TotalQuantity;
        data.TotalAmount += amount;

        if (reaction.Quantized)
            /// This makes it possible to have continuous quantized reactions. They will tick up in the background, they just won't do anything until they accumulate at least one unit reaction.
            stepAmt = ((int)data.TotalQuantity - (int)stepAmt);
        else
            stepAmt = data.TotalQuantity - stepAmt;

        if (stepAmt <= FixedPoint2.Epsilon)
        {   // Please don't let the reaction do anything unless we have enough oomph to change the solution.
            products = null;
            return false;
        }

        foreach(var reactant in reaction.Reactants)
        {
            if(!reactant.Catalyst)
                solution.RemoveReagent(reactant.Id, reactant.Amount * amount);
        }

        if (reaction.Products.Any())
        {
            products = new();
            foreach(var (productId, productAmount) in reaction.Products)
            {
                products.AddReagent(productId, productAmount * amount);
            }
            /// Note: This assumes that the products produced by a unit reaction have nonzero heat capacity.
            products.Temperature = reaction.ProductTemperature;
        }
        else
        {
            products = null;
            /// Word of warning, if the reaction doesn't produce any products and also produces heat the temperature will approach
            /// <see cref="float.PositiveInfinity"/> if the reaction consumes everything in the solution.
            solution.ThermalEnergy += reaction.HeatDelta * (float)amount;
        }


        var args = new ReagentEffectArgs(uid, null, solution, null, reaction, amount, frameTime, EntityManager, ReactionMethod.None, 1);
        RaiseLocalEvent(uid, ref args);

        if (reaction.StepEffects == null)
            return true;

        foreach (var effect in reaction.StepEffects)
        {
            if(!effect.ShouldApply(ref args, _random))
                continue;

            if (effect.ShouldLog)
            {
                _adminLogger.Add(
                    LogType.ReagentEffect,
                    effect.LogImpact,
                    $"Reaction effect {effect.GetType().Name:effect} of reaction ${reaction.ID:reaction} applied on entity {ToPrettyString(uid):entity} at {Transform(uid).Coordinates:coordinates}"
                );
            }

            effect.Effect(ref args);
        }

        return true;
    }

    /// <summary>
    /// Handles any effects a reaction has when the reaction stops reacting.
    /// </summary>
    /// <param name="reaction">The reaction that beginning to react.</param>
    /// <param name="uid">The entity that the reaction is beginning within.</param>
    /// <param name="solution">The <see cref="Solution"/> that the reaction is beginning within.</param>
    /// <param name="curTime">The reaction that continuing to react.</param>
    /// <param name="data">A wrapper for the state of the reaction within the solution created at the start of the reaction.</param>
    protected virtual void OnReactionStop(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, ReactionData data)
    {
        data.State = ReactionState.Stopped;
        if (reaction.StopEffects == null)
            return;

        var args = new ReagentEffectArgs(uid, null, solution, null, reaction, data.TotalQuantity, data.TotalTime, EntityManager, ReactionMethod.None, 1);
        RaiseLocalEvent(uid, ref args); // TODO: Make this handle reagent effect args.

        foreach (var effect in reaction.StopEffects)
        {
            if(!effect.ShouldApply(ref args, _random))
                continue;

            if (effect.ShouldLog)
            {
                _adminLogger.Add(
                    LogType.ReagentEffect,
                    effect.LogImpact,
                    $"Reaction effect {effect.GetType().Name:effect} of reaction ${reaction.ID:reaction} applied on entity {ToPrettyString(uid):entity} at {Transform(uid).Coordinates:coordinates}"
                );
            }

            effect.Effect(ref args);
        }
    }
}
