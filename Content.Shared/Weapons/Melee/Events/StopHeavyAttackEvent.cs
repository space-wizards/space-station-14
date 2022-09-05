namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised by the client if it pre-emptively stops a heavy attack.
/// </summary>
public sealed class StopHeavyAttackEvent : EntityEventArgs
{
    public readonly EntityUid Weapon;

    public StopHeavyAttackEvent(EntityUid weapon)
    {
        Weapon = weapon;
    }
}
