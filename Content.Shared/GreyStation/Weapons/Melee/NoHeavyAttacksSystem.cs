using Content.Shared.Weapons.Melee;

namespace Content.Shared.GreyStation.Weapons.Melee;

public sealed class NoHeavyAttacksSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoHeavyAttacksComponent, HeavyAttackAttemptEvent>(OnHeavyAttackAttempt);
    }

    private void OnHeavyAttackAttempt(Entity<NoHeavyAttacksComponent> ent, ref HeavyAttackAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
