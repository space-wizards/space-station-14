using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCookStart(Entity<ActiveMicrowaveComponent> ent, ref ComponentStartup args)
    {
        if (!_microwaveQuery.TryComp(ent, out var microwaveComponent))
            return;

        SetAppearance((ent, microwaveComponent), MicrowaveVisualState.Cooking);
        _powerState.SetWorkingState(ent.Owner, true);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (microwaveComponent.PlayingStream != null && microwaveComponent.PlayingStream != EntityUid.Invalid)
            return;

        var audioParams = AudioParams.Default.WithLoop(true).WithMaxDistance(5);
        var pvs = AudioSys.PlayPredicted(microwaveComponent.LoopingSound, ent, ent.Comp.User, audioParams);
        microwaveComponent.PlayingStream = pvs?.Entity;
        Dirty(ent, microwaveComponent);
    }

    /// <summary>
    ///     Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCookEnd(Entity<ActiveMicrowaveComponent> ent, ref ComponentShutdown args)
    {
        if (!_microwaveQuery.TryComp(ent, out var microwaveComponent))
            return;

        DeactivateMicrowaveCycle((ent, microwaveComponent));
    }

    /// <summary>
    ///     Adjusts a microwave's visuals, audio, and power draw when deactivated.
    /// </summary>
    private void DeactivateMicrowaveCycle(Entity<MicrowaveComponent> ent)
    {
        SetAppearance(ent.AsNullable(), MicrowaveVisualState.Idle);
        _powerState.SetWorkingState(ent.Owner, false);

        // TODO: Completely redo our Audio API and prediction because it doesn't work for VARIOUS reasons
        // TODO: See e#6722 for some details
        PredictedQueueDel(ent.Comp.PlayingStream);
        ent.Comp.PlayingStream = null;
        Dirty(ent);

        foreach (var solid in GetMicrowaveContents(ent.AsNullable()))
        {
            RemComp<ActivelyMicrowavedComponent>(solid);
        }
    }

    /// <summary>
    ///     Adds ActivelyMicrowavedComponent to entities inserted into an active microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(args.Entity);
        microwavedComp.Microwave = ent.Owner;
    }

    /// <summary>
    ///     Removes ActivelyMicrowavedComponent from entities removed from an active microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    /// <summary>
    ///     Prevents reagent reactions in entitites that are actively being microwaved.
    /// </summary>
    /// <remarks>
    ///     For example, raw egg would otherwise turn into cooked egg during the process, preventing it from being
    ///     "spent" when the microwave is finished cooking.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnReactionAttempt(Entity<ActivelyMicrowavedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (!TryComp<ActiveMicrowaveComponent>(ent.Comp.Microwave, out var activeMicrowaveComp))
            return;

        var portionedRecipe = activeMicrowaveComp.PortionedRecipe;
        if (portionedRecipe == null // no recipe selected
            || !ProtoMan.TryIndex(portionedRecipe.Value.Recipe, out var recipe))
            return;

        var recipeReagents = recipe.Ingredients.Reagents.Keys;

        foreach (var reagent in recipeReagents)
        {
            if (args.Event.Reaction.Reactants.ContainsKey(reagent))
            {
                args.Event.Cancelled = true;
                return;
            }
        }
    }
}
