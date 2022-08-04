using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.NPC.Components;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Combat;

/// <summary>
/// Handles melee combat for NPCs.
/// </summary>
public sealed class NPCMeleeCombatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
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
        component.Status = CombatStatus.TargetNormal;

        // TODO: Also need to co-ordinate with steering to keep in range.
        // For now I've just moved the utlity version over.
        // Also need some blackboard data for stuff like juke frequency, assigning target slots (to surround targets), etc.
        // miss %
        if (!TryComp<MeleeWeaponComponent>(component.Weapon, out var weapon))
        {
            component.Status = CombatStatus.NoWeapon;
            return;
        }

        if (weapon.CooldownEnd > _timing.CurTime)
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

        _interaction.DoAttack(component.Owner, targetXform.Coordinates, false, component.Target);

        if (TryComp<MobStateComponent>(component.Target, out var mobState))
        {
            component.Status = mobState.CurrentState switch
            {
                DamageState.Critical => CombatStatus.TargetCrit,
                DamageState.Dead => CombatStatus.TargetDead,
                _ => component.Status
            };
        }
    }
}
