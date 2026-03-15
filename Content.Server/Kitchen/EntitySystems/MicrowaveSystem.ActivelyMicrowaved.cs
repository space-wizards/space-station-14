using Content.Server.Construction;
using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    ///     Prevents construction graph operations as a result of temperature changes.
    /// </summary>
    /// <remarks>
    ///     For example: raw meat will not turn into steak while it is actively being microwaved.
    /// </remarks>
    /// <param name="ent">An entity that is actively being microwaved.</param>
    private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
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
