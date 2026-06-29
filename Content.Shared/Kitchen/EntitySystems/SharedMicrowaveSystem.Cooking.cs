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
    ///     Finishes a cooking operation in the microwave, resulting in a finished food recipe,
    ///     the ejection of all remaining ingredients, and a sound cue.
    /// </summary>
    /// <param name="ent">The micorawve entity.</param>
    private void CompleteCooking(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
    {
        var active = ent.Comp1;
        var microwave = ent.Comp2;
        var microwaveEnt = (ent.Owner, microwave);

        // Spawn a finished recipe, if there is one.
        if (active.PortionedRecipe.Recipe != null)
            ProduceFinishedRecipe(microwaveEnt, active.PortionedRecipe.Recipe, active.PortionedRecipe.Count);

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
