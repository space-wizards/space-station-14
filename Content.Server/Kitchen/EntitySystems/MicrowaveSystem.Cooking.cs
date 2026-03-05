using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Stacks;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    /// Starts Cooking
    /// </summary>
    /// <remarks>
    /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
    /// -emo
    /// </remarks>
    public void Wzhzhzh(EntityUid uid, MicrowaveComponent component, EntityUid? user)
    {
        if (!HasContents(component)
            || HasComp<ActiveMicrowaveComponent>(uid)
            || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
            return;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();
        var malfunctioning = false;
        // TODO use lists of Reagent quantities instead of reagent prototype ids.
        foreach (var item in component.Storage.ContainedEntities.ToArray())
        {
            // special behavior when being microwaved ;)
            var ev = new BeingMicrowavedEvent(uid, user);
            RaiseLocalEvent(item, ev);

            // TODO MICROWAVE SPARKS & EFFECTS
            // Various microwaveable entities should probably spawn a spark, play a sound, and generate a pop=up.
            // This should probably be handled by the microwave system, with fields in BeingMicrowavedEvent.

            if (ev.Handled)
            {
                UpdateUserInterfaceState(uid, component);
                return;
            }

            if (_tag.HasTag(item, MetalTag))
            {
                malfunctioning = true;
            }

            if (_tag.HasTag(item, PlasticTag))
            {
                var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                _container.Insert(junk, component.Storage);
                Del(item);
                continue;
            }

            var microwavedComp = AddComp<ActivelyMicrowavedComponent>(item);
            microwavedComp.Microwave = uid;

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
                continue;

            if (!solidsDict.TryAdd(solidID, amountToAdd))
                solidsDict[solidID] += amountToAdd;

            // only use reagents we have access to
            // you have to break the eggs before we can use them!
            if (!TryGetUsableIngredientSolution(item, out var _, out var solution))
                continue;

            foreach (var (reagent, quantity) in solution.Contents)
            {
                if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                    reagentDict[reagent.Prototype] += quantity;
            }
        }

        // Check recipes
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(uid, ref getRecipesEv);

        List<FoodRecipePrototype> recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);
        var portionedRecipe = recipes.Select(r =>
            CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

        _audio.PlayPvs(component.StartCookingSound, uid);
        var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
        activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
        activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
        activeComp.PortionedRecipe = portionedRecipe;
        //Scale tiems with cook times
        component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier);
        if (malfunctioning)
            activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
        UpdateUserInterfaceState(uid, component);
    }
}
