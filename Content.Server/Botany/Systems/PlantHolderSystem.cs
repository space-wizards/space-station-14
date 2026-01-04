using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Robust.Shared.Random;

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
        ent.Comp.Health = MathHelper.Clamp(ent.Comp.Health, 0f, plant.Endurance);
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
        ent.Comp.MutationLevel = MathHelper.Clamp(ent.Comp.MutationLevel, 0f, ent.Comp.MaxMutationLevel);
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
        ent.Comp.MutationMod = MathHelper.Clamp(ent.Comp.MutationMod, 0f, ent.Comp.MaxMutationMod);
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
        ent.Comp.PestLevel = MathHelper.Clamp(ent.Comp.PestLevel, 0f, ent.Comp.MaxPestLevel);
    }

    /// <summary>
    /// Adjusts the age of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsAge(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Age = Math.Max(0, ent.Comp.Age + amount);
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
        ent.Comp.Toxins = MathHelper.Clamp(ent.Comp.Toxins, 0f, ent.Comp.MaxToxins);
    }

    /// <summary>
    /// Adjusts the yield mod of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsYieldMod(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.YieldMod += amount;
        ent.Comp.YieldMod = MathHelper.Clamp(ent.Comp.YieldMod, 1, ent.Comp.MaxYieldMod);
    }

    /// <summary>
    /// Adjusts the skip aging of the plant.
    /// </summary>
    [PublicAPI]
    public void AdjustsSkipAging(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.SkipAging = Math.Max(0, ent.Comp.SkipAging + amount);
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
        ent.Comp.MutationLevel = 0;
        ent.Comp.YieldMod = 1;
        ent.Comp.MutationMod = 1;
    }
}
