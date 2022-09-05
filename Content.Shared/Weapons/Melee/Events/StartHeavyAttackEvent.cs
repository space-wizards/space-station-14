namespace Content.Shared.Weapons.Melee.Events;

public sealed class StartHeavyAttackEvent : EntityEventArgs
{
    public readonly EntityUid Weapon;

    public StartHeavyAttackEvent(EntityUid weapon)
    {
        Weapon = weapon;
    }
}
