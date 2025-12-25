using JetBrains.Annotations;
using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// API for runtime plant lifecycle state.
/// </summary>
public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly PlantSystem _plant = default!;

    /// <summary>
    /// Adjusts the health of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsHealth(Entity<PlantHolderComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!TryComp<PlantComponent>(ent.Owner, out var plant))
            return;

        ent.Comp.Health += amount;
        ent.Comp.Health = MathHelper.Clamp(ent.Comp.Health, 0, plant.Endurance);
        CheckHealth(ent);
        _plant.UpdateSprite(ent.Owner);
    }

    /// <summary>
    /// Adjusts the mutation level of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsMutationLevel(Entity<PlantHolderComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.MutationLevel += amount * ent.Comp.MutationMod;
        CheckHealth(ent);
    }

    /// <summary>
    /// Adjusts the mutation mod of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsMutationMod(Entity<PlantHolderComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.MutationMod += amount;
    }

    /// <summary>
    /// Adjusts the pests of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsPests(Entity<PlantHolderComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.PestLevel += amount;
    }

    /// <summary>
    /// Adjusts the age of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsAge(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Age += amount;
        _plant.UpdateSprite(ent.Owner);
    }

    /// <summary>
    /// Adjusts the toxins of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsToxins(Entity<PlantHolderComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Toxins += amount;
    }

    /// <summary>
    /// Checks if the plant is dead.
    /// </summary>
    [PublicAPI]
    public bool IsDead(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return ent.Comp.Dead;
    }

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
