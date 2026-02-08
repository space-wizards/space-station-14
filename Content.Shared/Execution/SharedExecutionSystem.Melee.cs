using Content.Shared.Weapons.Melee;

namespace Content.Shared.Execution;

public sealed partial class SharedExecutionSystem
{
    private void InitialiseMelee()
    {
        SubscribeLocalEvent<MeleeWeaponComponent, BeforeExecutionEvent>(OnBeforeExecutionMelee);
    }

    private void OnBeforeExecutionMelee(Entity<MeleeWeaponComponent> melee, ref BeforeExecutionEvent args)
    {
        if (args.Handled)
            return;

        args.Damage = melee.Comp.Damage;
        args.Sound = melee.Comp.HitSound;
        args.Handled = true;
    }
}
