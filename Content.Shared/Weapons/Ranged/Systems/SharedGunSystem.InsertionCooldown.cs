using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// Returns true if the insertion is allowed to go ahead, and updates the insertion delay if it does.
    /// </summary>
    public bool ValidateInsertionSpeed(Entity<AmmoProviderInsertionCooldownComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false)) // No comp? No problem!
            return true;

        if (IsInsertionTooFast(entity))
        {
            return false;
        }
        else
        {
            UpdateLastInsertion(entity);
            return true;
        }
    }

    /// <summary>
    /// Returns true if the insertion is happening too fast and should fail.
    /// </summary>
    public bool IsInsertionTooFast(Entity<AmmoProviderInsertionCooldownComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        return entity.Comp.LastInsertion + entity.Comp.InsertCooldown > Timing.CurTime;
    }

    /// <summary>
    /// Updates the last insertion value to the current time.
    /// </summary>
    public void UpdateLastInsertion(Entity<AmmoProviderInsertionCooldownComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        entity.Comp.LastInsertion = Timing.CurTime;
        DirtyField(entity.AsNullable(), nameof(AmmoProviderInsertionCooldownComponent.LastInsertion));
    }
}
