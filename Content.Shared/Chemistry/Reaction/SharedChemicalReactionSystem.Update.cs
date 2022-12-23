
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.Reaction;

public abstract partial class SharedChemicalReactionSystem
{
    /// <summary>
    /// Updates the progress and state of all chemical reactions everywhere.
    /// </summary>
    /// <param name="frameTime">The amount of time since the last time this subsystem was updated.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if(!_timing.IsFirstTimePredicted)
            return;

        foreach(var reacting in EntityQuery<ReactingComponent>())
        {
            var curTime = _timing.CurTime;
            if (reacting.NextUpdateTime <= curTime)
                Update(reacting.Owner, curTime, reacting);
        }
    }

    /// <summary>
    /// Updates the progress and state of the chemical reactions occurring within some target entity.
    /// </summary>
    /// <param name="uid">The entity to update the chemical reactions within.</param>
    /// <param name="allReactions">The set of chemical reactions occurring within the target entity.</param>
    /// <param name="mixer">The state of some entity being used to stir the solutions within the target entity.</param>
    public void Update(EntityUid uid, ReactingComponent? allReactions = null, ReactionMixerComponent? mixer = null)
    {
        if (Resolve(uid, ref allReactions))
            Update(uid, _timing.CurTime, allReactions);
    }

    /// <summary>
    /// Updates the progress and state of the chemical reactions occurring within some target entity.
    /// </summary>
    /// <param name="uid">The entity to update the chemical reactions within.</param>
    /// <param name="curTime">The time at which to update the relevant chemical reactions.</param>
    /// <param name="allReactions">The set of chemical reactions occurring within the target entity.</param>
    /// <param name="mixer">The state of some entity being used to stir the solutions within the target entity.</param>
    public void Update(EntityUid uid, TimeSpan curTime, ReactingComponent? allReactions = null, ReactionMixerComponent? mixer = null)
    {
        if(!Resolve(uid, ref allReactions))
            return;

        allReactions.LastUpdateTime = curTime;
        allReactions.NextUpdateTime = allReactions.LastUpdateTime + allReactions.TargetUpdatePeriod;
        foreach(var (solution, reactions) in allReactions.ReactionGroups)
        {
            HandleReactions(uid, solution, curTime, new SortedSet<ReactionSpecification>(reactions.Keys), reactions, allReactions);
        }

        if (allReactions.ReactionGroups.Count <= 0 && !allReactions.QueuedForDeletion)
        {
            allReactions.QueuedForDeletion = true;
            QueueLocalEvent(new AllReactionsStoppedMessage(uid, allReactions));
        }
    }

    /// <summary>
    /// Updates all of the reactions occurring within a target solution.
    /// Should be used by other subsystems if they modify the state of a solution.
    /// </summary>
    /// <param name="uid">The entity to update the reactions within.</param>
    /// <param name="solution">The solution to update the reactions within.</param>
    /// <param name="allReactions">All of the reactions occurring within the target entity.</param>
    /// <param name="mixer">The state of some entity being used to stir the target solution.</param>
    public void UpdateReactions(EntityUid uid, Solution solution, ReactingComponent? allReactions = null, ReactionMixerComponent? mixer = null)
        => UpdateReactions(uid, solution, _timing.CurTime, allReactions, mixer);

    /// <summary>
    /// Updates all of the reactions occurring within a target solution.
    /// Should be used by other subsystems if they modify the state of a solution.
    /// </summary>
    /// <param name="uid">The entity to update the reactions within.</param>
    /// <param name="solution">The solution to update the reactions within.</param>
    /// <param name="curTime">The time at which to update the reactions occurring within the target solution.</param>
    /// <param name="allReactions">The set of reactions occurring within the target entity.</param>
    /// <param name="mixer">The state of some entity being used to stir the target solution.</param>
    public void UpdateReactions(EntityUid uid, Solution solution, TimeSpan curTime, ReactingComponent? allReactions = null, ReactionMixerComponent? mixer = null)
    {
        SortedSet<ReactionSpecification> reactions = new();
        Dictionary<ReactionSpecification, ReactionData>? ongoing = null;
        if (Resolve(uid, ref allReactions, logMissing:false)
        &&  allReactions.ReactionGroups.TryGetValue(solution, out ongoing))
            reactions.UnionWith(ongoing.Keys);

        foreach(var (reagentId, reagentQuantity) in solution)
        {
            if (_reactions.TryGetValue(reagentId, out var reagentReactions))
                reactions.UnionWith(reagentReactions);
        }

        HandleReactions(uid, solution, curTime, reactions, ongoing, allReactions);
    }

    private sealed class AllReactionsStoppedMessage : EntityEventArgs
    {
        public readonly EntityUid Uid;
        public readonly ReactingComponent Comp;

        public AllReactionsStoppedMessage(EntityUid uid, ReactingComponent comp)
        {
            Uid = uid;
            Comp = comp;
        }
    }

    /// <summary>
    /// Handles removing a reaction tracking component if all reactions on the host entity have stopped.
    /// </summary>
    /// <param name="args">The uid and state of the reaction tracking component.</param>
    private void _onAllReactionsStopped(AllReactionsStoppedMessage args)
    {
        if (args.Comp.ReactionGroups.Count <= 0)
            RemComp(args.Uid, args.Comp);
        else
            args.Comp.QueuedForDeletion = false;
    }
}
