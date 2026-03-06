using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    private void CreateBurnedMess(Entity<MicrowaveComponent> microwave, EntityUid item)
    {
        var junk = Spawn(microwave.Comp.BadRecipeEntityId, Transform(microwave).Coordinates);
        _container.Insert(junk, microwave.Comp.Storage);

        Del(item);
    }

    private void SumItemIngredients(EntityUid item,
        Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> materials,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        if (TryGetSolidId(item, out var solidId))
        {
            if (!solids.TryAdd(solidId.Value, 1))
                solids[solidId.Value] += 1;
        }

        if (TryGetMaterialId(item, out var materialId, out var stack))
        {
            var count = stack.Value.Comp.Count;
            if (!materials.TryAdd(materialId.Value, count))
                materials[materialId.Value] += count;
        }

        if (TryGetUsableIngredientSolution(item, out var _, out var solution))
        {
            foreach (var (reagent, quantity) in solution.Contents)
            {
                if (!reagents.TryAdd(reagent.Prototype, quantity))
                    reagents[reagent.Prototype] += quantity;
            }
        }
    }

    private List<FoodRecipePrototype> GetRecipesForMicrowave(EntityUid microwave)
    {
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(microwave, ref getRecipesEv);

        var recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);

        return recipes;
    }

    private AvailableIngredients GetTotalIngredients(Entity<MicrowaveComponent> microwave, List<EntityUid> items)
    {
        var solids = new Dictionary<EntProtoId, int>();
        var materials = new Dictionary<ProtoId<StackPrototype>, int>();
        var reagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();

        foreach (var item in items)
        {
            SumItemIngredients(item, solids, materials, reagents);
            var activelyMicrowaved = AddComp<ActivelyMicrowavedComponent>(item);
            activelyMicrowaved.Microwave = microwave.Owner;
        }

        return new(solids, materials, reagents);
    }

    private bool ProcessContents(Entity<MicrowaveComponent> microwave,
        EntityUid? user,
        ref bool malfunctioning,
        [NotNullWhen(true)] out AvailableIngredients? available)
    {
        available = null;

        var microwaveContainer = microwave.Comp.Storage;
        var ingredientContents = microwaveContainer.ContainedEntities.ToList();

        foreach (var item in microwaveContainer.ContainedEntities)
        {
            // Special item-in-microwave interactions. Certain "being microwaved' interactions
            // may cancel out any actual cooking, so this may early exit.
            var beingMicrowaved = new BeingMicrowavedEvent(microwave.Owner, user);
            RaiseLocalEvent(item, beingMicrowaved);
            if (beingMicrowaved.Handled)
            {
                UpdateUserInterfaceState(microwave);
                return false;
            }

            // TODO: Whitelist
            if (_tag.HasTag(item, MetalTag))
                malfunctioning = true;

            // TODO: Whitelist
            if (_tag.HasTag(item, PlasticTag))
            {
                ingredientContents.Remove(item);
                CreateBurnedMess(microwave, item);
                continue;
            }
        }

        available = GetTotalIngredients(microwave, ingredientContents);
        return true;
    }

    private (FoodRecipePrototype? recipe, int count) GetRecipe(Entity<MicrowaveComponent> microwave, AvailableIngredients ingredients)
    {
        var recipes = GetRecipesForMicrowave(microwave.Owner);
        var cookTime = microwave.Comp.CurrentCookTimerTime;
        var recipePortions = recipes.Select(recipe =>
            {
                var portions = GetRecipePortions(recipe, cookTime, ingredients);
                return (recipe, portions);
            });

        return recipePortions.FirstOrNull(r => r.portions > 0)
            ?? (null, 0);
    }

    private void ActivateMicrowave(EntityUid uid,
        MicrowaveComponent component,
        (FoodRecipePrototype? recipe, int count) recipe,
        float cookTime,
        bool malfunctioning)
    {
        _audio.PlayPvs(component.StartCookingSound, uid);

        var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
        activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
        activeComp.CookTimeRemaining = cookTime;
        activeComp.PortionedRecipe = recipe;

        //Scale times with cook times
        component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(cookTime);

        if (malfunctioning)
            activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
    }

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

        var malfunctioning = false;
        if (!ProcessContents((uid, component), user, ref malfunctioning, out var ingredients))
            return;

        var recipe = GetRecipe((uid, component), ingredients.Value);
        var cookTime = component.CurrentCookTimerTime * component.CookTimeMultiplier;

        ActivateMicrowave(uid, component, recipe, cookTime, malfunctioning);
        UpdateUserInterfaceState(uid, component);
    }

    // TODO: there's actually a kind of nasty edge case microwave economics issue here,
    // all reagents / materials / solids will be included, but when the recipe is actually made,
    // solids are used first, then materials, then reagents.
    // thus, recipe detection might thing you have "more" ingredients than you actually do.
    //
    // moral of the story: I hate microwaves
    public readonly struct AvailableIngredients(Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> materials,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        public readonly Dictionary<EntProtoId, int> Solids = solids;
        public readonly Dictionary<ProtoId<StackPrototype>, int> Materials = materials;
        public readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = reagents;
    }
}
