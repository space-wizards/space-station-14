using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class MicrowaveSystem
{
    /// <summary>
    /// Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCookStart(Entity<ActiveMicrowaveComponent> ent, ref ComponentStartup args)
    {
        if (!MicrowaveQuery.TryComp(ent, out var microwaveComponent))
            return;

        ent.Comp.NextCookUpdate = Timing.CurTime + microwaveComponent.UpdateInterval;
        ent.Comp.LastCookUpdated = Timing.CurTime;
        DirtyFields(ent.AsNullable(), null, nameof(ent.Comp.NextCookUpdate), nameof(ent.Comp.LastCookUpdated));
        ActivateMicrowaveCycle((ent, microwaveComponent));
    }

    /// <summary>
    /// Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCookEnd(Entity<ActiveMicrowaveComponent> ent, ref ComponentShutdown args)
    {
        if (!MicrowaveQuery.TryComp(ent, out var microwaveComponent))
            return;

        DeactivateMicrowaveCycle((ent, microwaveComponent));
    }

    /// <summary>
    /// Adds ActivelyMicrowavedComponent to entities inserted into an active microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (Timing.ApplyingState)
            return;

        BeginActivelyMicrowaving(args.Entity, ent.Owner);
    }

    /// <summary>
    /// Removes ActivelyMicrowavedComponent from entities removed from an active microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (Timing.ApplyingState)
            return;

        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    /// <summary>
    /// Prevents reagent reactions in entitites that are actively being microwaved.
    /// </summary>
    /// <remarks>
    /// For example, raw egg would otherwise turn into cooked egg during the process, preventing it from being
    /// "spent" when the microwave is finished cooking.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnReactionAttempt(Entity<ActivelyMicrowavedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (!ActiveMicrowaveQuery.TryComp(ent.Comp.Microwave, out var activeMicrowaveComp))
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

    /// <summary>
    /// Adjusts a microwave's visuals, audio, and power draw when activated.
    /// </summary>
    protected virtual void ActivateMicrowaveCycle(Entity<MicrowaveComponent> ent)
    {
        SetAppearance(ent, MicrowaveVisualState.Cooking);
        _powerState.SetWorkingState(ent.Owner, true);
    }

    /// <summary>
    /// Adjusts a microwave's visuals, audio, and power draw when deactivated.
    /// </summary>
    protected virtual void DeactivateMicrowaveCycle(Entity<MicrowaveComponent> ent)
    {
        SetAppearance(ent, MicrowaveVisualState.Idle);
        _powerState.SetWorkingState(ent.Owner, false);
        foreach (var uid in GetMicrowaveContents(ent.AsNullable()))
        {
            RemCompDeferred<ActivelyMicrowavedComponent>(uid);
        }
    }

    /// <summary>
    /// Add ActivelyMicrowavedComponent to items that are being actively microwaved.
    /// </summary>
    /// <param name="uid">The entity being microwaved.</param>
    /// <param name="microwave">That dastardly microwave</param>
    private void BeginActivelyMicrowaving(EntityUid uid, EntityUid microwave)
    {
        var comp = new ActivelyMicrowavedComponent { Microwave = microwave };
        AddComp(uid, comp);
    }
}
