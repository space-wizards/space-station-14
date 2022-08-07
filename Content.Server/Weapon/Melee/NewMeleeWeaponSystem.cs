using Content.Shared.Weapons.Melee;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (_, comp) in EntityQuery<ActiveNewMeleeWeaponComponent, NewMeleeWeaponComponent>())
        {
            comp.WindupAccumulator = MathF.Min(comp.WindupTime, comp.WindupAccumulator + frameTime);
        }
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Pvs(uid.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    protected override void OnAttackStart(StartAttackEvent msg, EntitySessionEventArgs args)
    {
        base.OnAttackStart(msg, args);
        EnsureComp<ActiveNewMeleeWeaponComponent>(msg.Weapon);
    }

    protected override void StopAttack(StopAttackEvent ev, EntitySessionEventArgs args)
    {
        base.StopAttack(ev, args);
        // TODO: Check this.
        RemComp<ActiveNewMeleeWeaponComponent>(ev.Weapon);
    }

    protected override void OnReleaseAttack(ReleaseAttackEvent ev, EntitySessionEventArgs args)
    {
        base.OnReleaseAttack(ev, args);
        RemComp<ActiveNewMeleeWeaponComponent>(ev.Weapon);
    }
}
