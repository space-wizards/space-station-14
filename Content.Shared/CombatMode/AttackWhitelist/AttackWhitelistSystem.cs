using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Shared.CombatMode.AttackWhitelist;

public sealed partial class AttackWhitelistSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttackWhitelistComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<AttackWhitelistComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnAttackAttempt(Entity<AttackWhitelistComponent> entity, ref AttackAttemptEvent args)
    {
        if (CheckCancelAttack(entity, args.Target))
            args.Cancel();
    }

    private void OnShotAttempt(Entity<AttackWhitelistComponent> entity, ref ShotAttemptedEvent args)
    {
        if (CheckCancelAttack(entity, args.Target))
            args.Cancel();
    }

    /// <summary>
    /// Checks if a attacker with <see cref="AttackWhitelistComponent"/> can attack a target
    /// </summary>
    /// <param name="attacker">the attacker entity with <see cref="AttackWhitelistComponent"/></param>
    /// <param name="target">the target</param>
    /// <returns>true if it should cancel the attack, false it should continue the attack</returns>
    private bool CheckCancelAttack(Entity<AttackWhitelistComponent> attacker, EntityUid? target)
    {
        if (target == null)
            return false;

        if (_whitelist.CheckBoth(target, attacker.Comp.Blacklist, attacker.Comp.Whitelist))
            return false;

        if (attacker.Comp.FailedMessage != null)
            _popup.PopupClient(Loc.GetString(attacker.Comp.FailedMessage, ("target", target)), attacker);

        return true;
    }
}
