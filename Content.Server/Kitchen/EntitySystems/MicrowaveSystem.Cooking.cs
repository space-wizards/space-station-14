using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Shared.Kitchen;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    private void CreateBurnedMess(Entity<MicrowaveComponent> microwave, EntityUid item)
    {
        var junk = Spawn(microwave.Comp.BadRecipeEntityId, Transform(microwave).Coordinates);
        _container.Insert(junk, microwave.Comp.Storage);

        Del(item);
    }

    private void MicrowaveItem(Entity<MicrowaveComponent> microwave,
        EntityUid item,
        EntityUid? user,
        ref bool malfunctioning,
        out bool shouldStopMicrowave,
        out bool shouldRemoveFromContents)
    {
        shouldStopMicrowave = false;
        shouldRemoveFromContents = false;

        // Special item-in-microwave interactions. Certain "being microwaved' interactions
        // may cancel out any actual cooking, so this may early exit.
        var beingMicrowaved = new BeingMicrowavedEvent(microwave.Owner, user);
        RaiseLocalEvent(item, beingMicrowaved);
        if (beingMicrowaved.Handled)
        {
            UpdateUserInterfaceState(microwave);
            shouldStopMicrowave = true;
            return;
        }

        if (_whitelist.IsWhitelistPass(microwave.Comp.MalfunctionWhenCookedWhitelist, item))
            malfunctioning = true;

        if (_whitelist.IsWhitelistPass(microwave.Comp.BurnWhenCookedWhitelist, item))
        {
            shouldRemoveFromContents = true;
            CreateBurnedMess(microwave, item);
        }
    }

    private bool ProcessContents(Entity<MicrowaveComponent> microwave,
        IReadOnlyList<EntityUid> contents,
        EntityUid? user,
        ref bool malfunctioning,
        out List<EntityUid> ingredientContents)
    {
        ingredientContents = [.. contents];

        foreach (var item in contents)
        {
            MicrowaveItem(microwave,
                item,
                user,
                ref malfunctioning,
                out var shouldExit,
                out var shouldRemoveFromContents);

            if (shouldExit)
                return false;

            if (shouldRemoveFromContents)
                ingredientContents.Remove(item);
        }

        foreach (var item in ingredientContents)
        {
            var activelyMicrowaved = AddComp<ActivelyMicrowavedComponent>(item);
            activelyMicrowaved.Microwave = microwave.Owner;
        }

        return true;
    }

    private void ActivateMicrowave(Entity<MicrowaveComponent> microwave,
        (FoodRecipePrototype? recipe, uint count) recipe,
        bool malfunctioning)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;

        _audio.PlayPvs(component.StartCookingSound, uid);

        var cookTime = component.CurrentCookTimerTime * component.CookTimeMultiplier;
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
    public void Wzhzhzh(Entity<MicrowaveComponent> microwave, EntityUid? user)
    {
        if (!HasContents(microwave)
            || HasComp<ActiveMicrowaveComponent>(microwave)
            || !_power.IsPowered(microwave.Owner))
            return;

        var contents = microwave.Comp.Storage.ContainedEntities;
        var malfunctioning = false;

        if (!ProcessContents(microwave,
            contents,
            user,
            ref malfunctioning,
            out var ingredientContents))
            return;

        var ingredients = GetTotalIngredients(microwave, ingredientContents);
        var recipe = GetRecipe(microwave, ingredients);

        ActivateMicrowave(microwave, recipe, malfunctioning);
        UpdateUserInterfaceState(microwave);
    }
}
