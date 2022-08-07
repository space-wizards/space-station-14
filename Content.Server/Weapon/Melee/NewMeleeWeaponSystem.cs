using Content.Shared.Weapons.Melee;

namespace Content.Server.Weapon.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (_, comp) in EntityQuery<ActiveNewMeleeWeaponComponent, NewMeleeWeaponComponent>())
        {
            comp.WindupAccumulator += frameTime;
        }
    }

    protected override void OnAttackStart(StartAttackEvent msg, EntitySessionEventArgs args)
    {
        base.OnAttackStart(msg, args);
        EnsureComp<ActiveNewMeleeWeaponComponent>(msg.Weapon);
    }
}
