using Content.Server.CombatMode;
using Content.Server.NPC.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Weapons.Melee;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    private void InitializeMelee()
    {
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentStartup>(OnMeleeStartup);
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentShutdown>(OnMeleeShutdown);
    }

    private void OnMeleeShutdown(EntityUid uid, NPCMeleeCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            combatMode.IsInCombatMode = false;
        }
    }

    private void OnMeleeStartup(EntityUid uid, NPCMeleeCombatComponent component, ComponentStartup args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            combatMode.IsInCombatMode = true;
        }

        // TODO: Cleanup later, just looking for parity for now.
        component.Weapon = uid;
    }

    private void UpdateMelee(float frameTime)
    {
        var combatQuery = GetEntityQuery<CombatModeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var (comp, _) in EntityQuery<NPCMeleeCombatComponent, ActiveNPCComponent>())
        {
            if (!combatQuery.TryGetComponent(comp.Owner, out var combat) || !combat.IsInCombatMode)
            {
                RemComp<NPCMeleeCombatComponent>(comp.Owner);
                continue;
            }

            Attack(comp, xformQuery);
        }
    }

    private void Attack(NPCMeleeCombatComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        component.Status = CombatStatus.Normal;

        // TODO: Also need to co-ordinate with steering to keep in range.
        // For now I've just moved the utlity version over.
        // Also need some blackboard data for stuff like juke frequency, assigning target slots (to surround targets), etc.
        // miss %
        if (!TryComp<MeleeWeaponComponent>(component.Weapon, out var weapon))
        {
            component.Status = CombatStatus.NoWeapon;
            return;
        }

        if (weapon.NextAttack > _timing.CurTime)
        {
            return;
        }

        if (!xformQuery.TryGetComponent(component.Owner, out var xform) ||
            !xformQuery.TryGetComponent(component.Target, out var targetXform))
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (!xform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var distance) ||
            distance > weapon.Range)
        {
            // TODO: Steering in combat.
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        _melee.AttemptLightAttack(component.Owner, weapon, component.Target);
    }
}
