
// Overlaps with existing namespace.
using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Shared.Kitchen.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Stacks;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem : EntitySystem
{
    /// <summary>
    /// Runs a set of minimal pre-reqs to check the timing, needed to enable dummy-proof, one-button interface to Wzhzhzh
    /// </summary>
    public void TryStartAssembly(EntityUid uid, MicrowaveComponent component, AssemblerStartCookMessage args)
    {
        if (!HasContents(component) || HasComp<ActiveMicrowaveComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
            return;

        var user = args.Actor;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();
        var malfunctioning = false;
        // TODO use lists of Reagent quantities instead of reagent prototype ids.
        foreach (var item in component.Storage.ContainedEntities.ToArray())
        {
            string? solidID = null;
            int amountToAdd = 1;

            // If a microwave recipe uses a stacked item, use the default stack prototype id instead of prototype id
            if (TryComp<StackComponent>(item, out var stackComp))
            {
                solidID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                amountToAdd = stackComp.Count;
            }
            else
            {
                var metaData = MetaData(item); //this simply begs for cooking refactor
                if (metaData.EntityPrototype is not null)
                    solidID = metaData.EntityPrototype.ID;
            }

            if (solidID is null)
            {
                continue;
            }


            if (solidsDict.ContainsKey(solidID))
            {
                solidsDict[solidID] += amountToAdd;
            }
            else
            {
                solidsDict.Add(solidID, amountToAdd);
            }

            if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                continue;

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
            {
                var solution = soln.Comp.Solution;
                foreach (var (reagent, quantity) in solution.Contents)
                {
                    if (reagentDict.ContainsKey(reagent.Prototype))
                        reagentDict[reagent.Prototype] += quantity;
                    else
                        reagentDict.Add(reagent.Prototype, quantity);
                }
            }
        }

        // Check recipes
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(uid, ref getRecipesEv);

        List<FoodRecipePrototype> recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);
        var portionedRecipe = recipes.Select(r =>
            GetPortionsForRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

        if (portionedRecipe.Item2 <= 0)
        {
            // Display popup
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-assembler-no-valid-recipe"), uid);
            _audio.PlayPvs(component.NoRecipeSound, uid);
            return;
        }

        // We're actually microwaving things, run the wzhzhzh checks
        foreach (var item in component.Storage.ContainedEntities.ToArray())
        {
            var ev = new BeingMicrowavedEvent(uid, user, component.CanHeat, component.CanIrradiate);
            RaiseLocalEvent(item, ev);

            if (ev.Handled)
            {
                UpdateUserInterfaceState(uid, component);
                return;
            }

            if (_tag.HasTag(item, MetalTag) && component.CanIrradiate)
            {
                malfunctioning = true;
            }

            if (_tag.HasTag(item, PlasticTag) && (component.CanHeat || component.CanIrradiate))
            {
                var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                _container.Insert(junk, component.Storage);
                Del(item);
                continue;
            }

            AddComp<ActivelyMicrowavedComponent>(item);
        }

        _audio.PlayPvs(component.StartCookingSound, uid);
        var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
        component.CurrentCookTimerTime = (uint)portionedRecipe.Item2 * portionedRecipe.Item1.CookTime;
        activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
        activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
        activeComp.PortionedRecipe = portionedRecipe;
        //Scale times with cook times
        component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier); // Frontier: CookTimeMultiplier<FinalCookTimeMultiplier
        if (malfunctioning)
            activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
        UpdateUserInterfaceState(uid, component);
    }

    /// <summary>
    /// Frontier: gets the largest number of portions
    /// </summary>
    public static (FoodRecipePrototype, int) GetPortionsForRecipe(MicrowaveComponent component, FoodRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents)
    {
        var portions = 0;

        // Frontier: microwave recipe machine types
        if ((recipe.RecipeType & component.ValidRecipeTypes) == 0)
        {
            return (recipe, 0);
        }
        // End Frontier

        foreach (var solid in recipe.IngredientsSolids)
        {
            if (!solids.ContainsKey(solid.Key))
                return (recipe, 0);

            if (solids[solid.Key] < solid.Value)
                return (recipe, 0);

            portions = portions == 0
                ? solids[solid.Key] / solid.Value.Int()
                : Math.Min(portions, solids[solid.Key] / solid.Value.Int());
        }

        foreach (var reagent in recipe.IngredientsReagents)
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]
            if (!reagents.ContainsKey(reagent.Key))
                return (recipe, 0);

            if (reagents[reagent.Key] < reagent.Value)
                return (recipe, 0);

            portions = portions == 0
                ? reagents[reagent.Key].Int() / reagent.Value.Int()
                : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
        }

        //Return as many portions as we can assemble with the given materials
        return (recipe, portions);
    }
}
