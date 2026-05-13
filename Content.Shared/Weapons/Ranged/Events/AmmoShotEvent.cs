namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when projectiles have been fired from it.
/// </summary>
public sealed partial class AmmoShotEvent : EntityEventArgs
{
    public List<EntityUid> FiredProjectiles = default!;
}

