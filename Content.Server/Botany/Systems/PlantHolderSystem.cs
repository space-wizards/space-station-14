using JetBrains.Annotations;
using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// API for runtime plant lifecycle state.
/// </summary>
public sealed class PlantHolderSystem : EntitySystem
{
    /// <summary>
    /// Checks if the plant is dead.
    /// </summary>
    [PublicAPI]
    public void CheckHealth(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Health <= 0)
            Die(ent);
    }

    /// <summary>
    /// Kills the plant.
    /// </summary>
    [PublicAPI]
    public void Die(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Dead = true;
        ent.Comp.Health = Math.Max(0, ent.Comp.Health);

        if (TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
            harvest.ReadyForHarvest = false;

        ent.Comp.MutationLevel = 0;
        ent.Comp.YieldMod = 1;
        ent.Comp.MutationMod = 1;
    }
}
