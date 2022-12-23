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

            if (data!.State == ReactionState.Starting) // The ! is necessary because [MaybeNullWhen(false)] does not override the inherent nullability of a type? argument.
            {
                ongoing ??= new();
                ongoing.Add(reaction, data);
            }

            result = true;
            break;
        }

        reactions.ExceptWith(toRemove);

        if (products == null)
            return result;

        foreach(var product in products)
        {
            if (_reactions.TryGetValue(product.ReagentId, out var productReactions))
                reactions.UnionWith(productReactions);
        }

        solution.AddSolution(products);
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
        else
            data.State = ReactionState.Running; // Do this here so the last cycle un-cancels continuous reactions.

        if (amount <= FixedPoint2.Zero)
            return false;

        return OnReactionStep(reaction, uid, solution, curTime, amount, data, out products);
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
    protected bool CanReact(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, ReactionData? data, out FixedPoint2 amount, ReactionMixerComponent? mixer = null)
    {
        amount = FixedPoint2.MaxValue;
        if (reaction.MinimumTemperature > float.NegativeInfinity
        ||  reaction.MaximumTemperature < float.PositiveInfinity)
        {
            if (solution.Temperature < reaction.MinimumTemperature)
            {
                amount = FixedPoint2.Zero;
                return false;
            } else if(solution.Temperature > reaction.MaximumTemperature)
            {
                amount = FixedPoint2.Zero;
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
                    if (limit >= FixedPoint2.Zero && limit < amount)
                    {   // If the limit is negative then the reaction will bring the solution further away from the thermal limit as it progresses.
                        amount = limit;
                        if (amount <= FixedPoint2.Zero)
                            return false;
                    }
                }

                divisor = dU - reaction.MaximumTemperature * dC;
                if (divisor != 0)
                {
                    limit = (reaction.MaximumTemperature * C0 - T0C0) / divisor;
                    if (limit >= FixedPoint2.Zero && limit < amount)
                    {   // If the limit is negative then the reaction will bring the solution further away from the thermal limit as it progresses.
                        amount = limit;
                        if (amount <= FixedPoint2.Zero)
                            return false;
                    }
                }
            }
        }

        if(!reaction.CanOverflow && reaction.VolumeDelta > FixedPoint2.Zero)
        {
            var limit = solution.AvailableVolume / reaction.VolumeDelta;
            if (limit < amount)
            {
                amount = limit;
                if (amount <= FixedPoint2.Zero)
                    return false;
            }
        }

        // This is so continuous mixing can work.
        List<string> currentlyMixingTypes = mixer != null ? new(mixer.ReactionTypes) : new();
        var attempt = new ReactionAttemptEvent(reaction, solution, uid, currentlyMixingTypes);
        RaiseLocalEvent(uid, attempt, false);
        if (attempt.Cancelled)
        {
            amount = FixedPoint2.Zero;
            return false;
        }

        if (reaction.MixingCategories != null
        && (data == null || reaction.NeedsContinuousMixing)
        && (!currentlyMixingTypes.Any() || reaction.MixingCategories.Except(currentlyMixingTypes).Any()))
        {
            amount = FixedPoint2.Zero;
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

            var unitReactions = reactantQuantity / reactantCoefficient;

            if (unitReactions < amount)
                amount = unitReactions;
        }

        if(!float.IsFinite(reaction.ReactionRate))
        {   // Infinitely fast reactions are considered as completing in a single update even with a timestep of size 0.
            if (reaction.Quantized)
                amount = (int) amount;
            return amount > FixedPoint2.Zero;
        }
        else if(amount <= FixedPoint2.Zero)
            return false;

        if (reaction.Quantized && amount < 1)
            return false; // While we want continuous quantized reactions to creep up on their target not having this check would permit them to creep up while not actually having enough reagents to complete a step.

        TimeSpan frameTime = data != null ? (curTime - data.LastTime) : TimeSpan.Zero;
        var rateLimit = reaction.ReactionRate * frameTime.TotalSeconds;
        if (rateLimit <= amount)
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
    protected void OnReactionStart(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, out ReactionData data)
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
    protected bool OnReactionStep(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, FixedPoint2 amount, ReactionData data, out Solution? products)
    {
        TimeSpan frameTime = curTime - data.LastTime;
        data.LastTime = curTime;

        if (reaction.Quantized)
        {   // Quantized reactions can only really occur in steps size n in N. This makes them compatible with continuous reactions by having the continuous reaction very slowly happen in the background and then the effects of the reaction only occur when they have a total reaction rate of at least 1.
            var oldQuantity = (int) data.TotalQuantity;
            data.TotalQuantity += amount;
            amount = ((int)data.TotalQuantity) - oldQuantity;
            if (amount <= FixedPoint2.Zero)
            {
                products = null;
                return false;
            }
        }
        else
            data.TotalQuantity += amount;

        foreach(var reactant in reaction.Reactants)
        {
            if(!reactant.Catalyst)
                solution.RemoveReagent(reactant.Id, reactant.Amount * amount);
        }

        products = new();
        if (reaction.Products.Any())
        {
            foreach(var (productId, productAmount) in reaction.Products)
            {
                products.AddReagent(productId, productAmount * amount);
            }
            products.Temperature = reaction.ProductTemperature;
        }
        else
            /// Word of warning, if the reaction doesn't produce any products and also produces heat the temperature will approach
            /// <see cref="float.PositiveInfinity"/> if the reaction consumes everything in the solution.
            solution.ThermalEnergy += reaction.HeatDelta * (float)amount;


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
    protected void OnReactionStop(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, ReactionData data)
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
