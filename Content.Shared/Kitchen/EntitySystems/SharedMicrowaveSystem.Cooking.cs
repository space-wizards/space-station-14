using Content.Shared.Kitchen.Components;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Adds temperature to every item in the microwave based on the time it took to microwave.
    /// </summary>
    /// <param name="component">The microwave that is heating up.</param>
    /// <param name="time">The heating time that has elapsed, in seconds.</param>
    protected virtual void AddTemperature(Entity<MicrowaveComponent> ent, float time)
    { }

    /// <summary>
    ///     Attempts to roll random "malfunction" events on a malfunctioning microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    protected virtual void RollMalfunction(Entity<MicrowaveComponent> ent)
    { }

    /// <summary>
    ///     Start up the microwave. This processes each item in the microwave to experience microwave "on-cooking"
    ///     effects, attempts to retrieve a microwave recipe with its valid ingredients, and activates the
    ///     microwave visually.
    /// </summary>
    public void StartCooking(Entity<MicrowaveComponent> microwave, EntityUid? user)
    {
        if (!HasContents(microwave.AsNullable())
            || IsActiveMicrowave(microwave.AsNullable())
            || !_power.IsPowered(microwave.Owner))
            return;

        var contents = microwave.Comp.Storage.ContainedEntities;
        var malfunctioning = false;
        var shouldContinue = TryStartMicrowavingContents(microwave,
            contents,
            user,
            ref malfunctioning,
            out var ingredientContents);

        if (!shouldContinue)
            return;

        var ingredients = GetTotalIngredients(ingredientContents);
        var cookTime = microwave.Comp.CurrentCookTimerTime;
        var recipe = GetRecipe(microwave, ingredients, cookTime);

        ActivateMicrowave(microwave, recipe, malfunctioning);
        UpdateUserInterfaceState(microwave.AsNullable());
    }

    /// <summary>
    ///     Starts up the microwave cooking operation, setting the starting time and recipe of the microwave.
    /// </summary>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="recipe">The recipe and portion count associated with this operaton.</param>
    /// <param name="malfunctioning">Whether or not this microwave is malfunctioning.</param>
    private void ActivateMicrowave(Entity<MicrowaveComponent> microwave,
        (FoodRecipePrototype? recipe, uint count) recipe,
        bool malfunctioning)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;

        Audio.PlayPvs(component.StartCookingSound, uid);

        var cookTime = component.CurrentCookTimerTime * component.CookTimeMultiplier;
        var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
        activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
        activeComp.PortionedRecipe = recipe;

        if (malfunctioning)
            activeComp.NextMalfunction = _timing.CurTime + component.MalfunctionInterval;
    }

    /// <summary>
    ///     Iterates over the contents of a microwave and performs some on-microwaved effects.
    ///     Plastic items will melt and turn into burned messes.
    ///     Metal items will cause the microwave to malfunction.
    /// </summary>
    /// <remarks>
    ///     This also raises a BeingMicrowavedEvent on each item in the microwave. The result of
    ///     this event may cause us to exit early and proceed with cooking - for example, an
    ///     entity that causes the microwave to explode when microwaved.
    ///
    ///     If cooking is not cancelled, the items inside will gain `ActiveMicrowaveComponent.`.
    /// </remarks>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="contents">The contents of the microwave.</param>
    /// <param name="user">The entity that is activating the microwave.</param>
    /// <param name="malfunctioning">Whether or not the microwave is malfunctioning.</param>
    /// <param name="ingredientContents">A list of entities usable as ingredients.</param>
    /// <returns>Wheteher or not we can proceed to use the microwave.</returns>
    private bool TryStartMicrowavingContents(Entity<MicrowaveComponent> microwave,
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
                out var shouldRemoveFromIngredients);

            if (shouldExit)
                return false;

            if (shouldRemoveFromIngredients)
                ingredientContents.Remove(item);
        }

        foreach (var item in ingredientContents)
        {
            var activelyMicrowaved = AddComp<ActivelyMicrowavedComponent>(item);
            activelyMicrowaved.Microwave = microwave.Owner;
        }

        return true;
    }

    /// <summary>
    ///     Processes a single entity in the microwave. This modifies / results in three
    ///     different parameters related to the operation: if the microwave should malfunction,
    ///     if the microwave should stop, and if the entity is still usable as an ingredient.
    /// </summary>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="item">The entity being microwaved.</param>
    /// <param name="user">The entity activating the microwave.</param>
    /// <param name="malfunctioning">Whether or not this microwave is malfunctioning.</param>
    /// <param name="shouldStopMicrowave">Whether or not this microwave should stop.</param>
    /// <param name="shouldRemoveFromIngredients">Whether or not this entity remains a valid ingredient.</param>
    private void MicrowaveItem(Entity<MicrowaveComponent> microwave,
        EntityUid item,
        EntityUid? user,
        ref bool malfunctioning,
        out bool shouldStopMicrowave,
        out bool shouldRemoveFromIngredients)
    {
        shouldStopMicrowave = false;
        shouldRemoveFromIngredients = false;

        // Special item-in-microwave interactions. Certain "being microwaved' interactions
        // may cancel out any actual cooking, so this may early exit.
        var beingMicrowaved = new BeingMicrowavedEvent(microwave.Owner, user);
        RaiseLocalEvent(item, beingMicrowaved);
        if (beingMicrowaved.Handled)
        {
            UpdateUserInterfaceState(microwave.AsNullable());
            shouldStopMicrowave = true;
            return;
        }

        if (_whitelist.IsWhitelistPass(microwave.Comp.MalfunctionWhenCookedWhitelist, item))
            malfunctioning = true;

        if (_whitelist.IsWhitelistPass(microwave.Comp.BurnWhenCookedWhitelist, item))
        {
            shouldRemoveFromIngredients = true;
            CreateBurnedMess(microwave, item);
        }
    }

    /// <summary>
    ///     Turns a single entity in the microwave into a failed "burned mess" recipe.
    /// </summary>
    /// <remarks>
    ///     This happens to entities that pass <see cref="MicrowaveComponent.BurnWhenCookedWhitelist"/>
    ///     when microwaved.
    /// </remarks>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="item">The entity to burn.</param>
    private void CreateBurnedMess(Entity<MicrowaveComponent> microwave, EntityUid item)
    {
        var entProto = microwave.Comp.BadRecipeEntityId;
        var coords = Transform(microwave).Coordinates;
        var junk = PredictedSpawnAtPosition(entProto, coords);
        _container.Insert(junk, microwave.Comp.Storage);

        PredictedDel(item);
    }

    /// <summary>
    ///     Completes a recipe in a microwave, removing its relevant ingredient contents
    ///     from the microwave and producing finished dish entities in their place.
    /// </summary>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="recipe">The recipe we are using.</param>
    /// <param name="count">The number of recipe portions to produce.</param>
    private void SpawnFinishedRecipe(Entity<MicrowaveComponent> microwave,
        FoodRecipePrototype recipe,
        uint count = 1)
    {
        SubtractContents(microwave, recipe, count);

        var coords = Transform(microwave).Coordinates;
        for (var i = 0; i < count; i++)
            Spawn(recipe.Result, coords);
    }

    /// <summary>
    ///     Finishes a cooking operation in the microwave, resulting in a finished food recipe,
    ///     the ejection of all remaining ingredients, and a sound cue.
    /// </summary>
    /// <param name="ent">The micorawve entity.</param>
    private void CompleteCooking(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
    {
        var active = ent.Comp1;
        var microwave = ent.Comp2;
        var microwaveEnt = (ent.Owner, microwave);
        var recipe = active.PortionedRecipe;

        // Spawn a finished recipe, if there is one.
        if (recipe.Recipe != null)
            SpawnFinishedRecipe(microwaveEnt, recipe.Recipe, recipe.Count);

        Audio.PlayPredicted(microwave.FoodDoneSound, ent, null); // beep... beep... beep
        UpdateUserInterfaceState(microwaveEnt);

        // Clean up the microwave.
        _container.EmptyContainer(microwave.Storage);
        StopCooking(microwaveEnt);
    }

    /// <summary>
    ///     Removes components from a microwave and its contents related to active microwave use.
    /// </summary>
    /// <remarks>
    ///     When the ActiveMicrowaveComponent is removed, it will trigger <see cref="OnCookStop"/> on shutdown.
    /// </remarks>
    /// <param name="ent">The microwave entity.</param>
    private void StopCooking(Entity<MicrowaveComponent> ent)
    {
        RemCompDeferred<ActiveMicrowaveComponent>(ent);

        foreach (var solid in ent.Comp.Storage.ContainedEntities)
            RemCompDeferred<ActivelyMicrowavedComponent>(solid);
    }
}
