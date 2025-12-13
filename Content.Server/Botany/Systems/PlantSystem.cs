using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies plant trait effects on growth ticks.
/// </summary>
public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<PlantComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? holder = null;
        if (!Resolve(uid, ref holder))
            return;

        // Check if plant is too old.
        if (holder.Age > component.Lifespan)
        {
            holder.Health -= _random.Next(3, 5) * BasicGrowthSystem.HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
    }

    /// <summary>
    /// Adjusts the potency of a plant component.
    /// </summary>
    public void AdjustPotency(Entity<PlantComponent> ent, float delta)
    {
        var plant = ent.Comp;
        plant.Potency = Math.Max(plant.Potency + delta, 1);
        Dirty(ent);
    }
}
