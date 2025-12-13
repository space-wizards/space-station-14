using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies plant trait effects on growth ticks.
/// </summary>
public sealed class PlantTraitsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantTraitsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? holder = null;
        if (!Resolve(uid, ref holder))
            return;

        if (holder.Seed == null || holder.Dead)
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
    /// Adjusts the potency of a plant traits component.
    /// </summary>
    public void AdjustPotency(Entity<PlantTraitsComponent> ent, float delta)
    {
        var traits = ent.Comp;
        traits.Potency = Math.Max(traits.Potency + delta, 1);
        Dirty(ent);
    }
}
