using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Subscribe to events related to active microwaves.
    /// </summary>
    private void InitializeActive()
    {
        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentStartup>(OnCookStart);
        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentShutdown>(OnCookStop);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);
    }

    /// <summary>
    ///     Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnCookStart(Entity<ActiveMicrowaveComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;

        SetAppearance((ent, microwaveComponent), MicrowaveVisualState.Cooking);
        var audioParams = AudioParams.Default.WithLoop(true).WithMaxDistance(5);
        var pvs = Audio.PlayPredicted(microwaveComponent.LoopingSound, ent, null, audioParams);
        microwaveComponent.PlayingStream = pvs?.Entity;

        _powerState.SetWorkingState(ent.Owner, true);
    }

    /// <summary>
    ///     Adjusts a microwave's visuals, audio, and power draw when deactivated.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnCookStop(Entity<ActiveMicrowaveComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;

        SetAppearance((ent, microwaveComponent), MicrowaveVisualState.Idle);
        microwaveComponent.PlayingStream = Audio.Stop(microwaveComponent.PlayingStream);

        _powerState.SetWorkingState(ent.Owner, false);
    }

    /// <summary>
    ///     Adds ActivelyMicrowavedComponent to entities inserted into an active microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(args.Entity);
        microwavedComp.Microwave = ent.Owner;
    }

    /// <summary>
    ///     Removes ActivelyMicrowavedComponent from entities removed from an active microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    /// <summary>
    ///     Prevents reagent reactions in entitites that are actively being microwaved.
    /// </summary>
    /// <remarks>
    ///     For example, raw egg would otherwise turn into cooked egg during the process, preventing it from being
    ///     "spent" when the microwave is finished cooking.
    /// </remarks>
    /// <param name="ent">An entity that is actively being microwaved.</param>
    private void OnReactionAttempt(Entity<ActivelyMicrowavedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (!TryComp<ActiveMicrowaveComponent>(ent.Comp.Microwave, out var activeMicrowaveComp))
            return;

        if (activeMicrowaveComp.PortionedRecipe.Recipe == null) // no recipe selected
            return;

        var recipeReagents = activeMicrowaveComp.PortionedRecipe.Recipe.Ingredients.Reagents.Keys;

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
