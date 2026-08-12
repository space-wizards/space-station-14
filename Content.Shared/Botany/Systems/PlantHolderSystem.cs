using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage.Systems;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// API for runtime plant lifecycle state.
/// </summary>
public sealed partial class PlantHolderSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;

    [SubscribeLocalEvent]
    private void OnCloning(Entity<PlantHolderComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        var cloneComp = EnsureComp<PlantHolderComponent>(args.CloneUid);
        _serialization.CopyTo(ent.Comp, ref cloneComp, notNullableOverride: true);
        Dirty(args.CloneUid, cloneComp);
    }

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<PlantHolderComponent> ent, ref DamageDealtEvent args)
    {
        AdjustsHealth(ent.AsNullable(), -args.Damage.GetTotal().Float());
    }

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
        DirtyField(ent, nameof(ent.Comp.Health));
        CheckHealth(ent);
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
        DirtyField(ent, nameof(ent.Comp.MutationLevel));
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
        DirtyField(ent, nameof(ent.Comp.MutationMod));
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
        DirtyField(ent, nameof(ent.Comp.Age));
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
        DirtyField(ent, nameof(ent.Comp.YieldMod));
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
        DirtyField(ent, nameof(ent.Comp.SkipAging));
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
            KillPlant(ent);
    }

    /// <summary>
    /// Kills the plant.
    /// </summary>
    [PublicAPI]
    public void KillPlant(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Dead = true;
        ent.Comp.Health = Math.Max(0, ent.Comp.Health);
        DirtyFields(ent, null, nameof(ent.Comp.Dead), nameof(ent.Comp.Health));
    }

    /// <summary>
    /// Checks whether the plant's health is at or below half its endurance.
    /// </summary>
    [PublicAPI]
    public bool GetHealthThreshold(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false)
            || !TryComp<PlantComponent>(ent.Owner, out var plant))
            return false;

        return ent.Comp.Health <= plant.Endurance * 0.5f;
    }
}
