namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised to get the effective shooting entity.
/// </summary>
[ByRefEvent]
public struct GetShootingEntityEvent
{
    public EntityUid? ShootingEntity;

    public bool Handled;
}
