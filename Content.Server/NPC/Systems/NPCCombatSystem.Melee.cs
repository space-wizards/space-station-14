using System.Numerics;
using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    private const float TargetMeleeLostRange = 14f;

    private void InitializeMelee()
    {
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentStartup>(OnMeleeStartup);
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentShutdown>(OnMeleeShutdown);
    }

    private void OnMeleeShutdown(EntityUid uid, NPCMeleeCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, false, combatMode);
        }

        _steering.Unregister(uid);
    }

    private void OnMeleeStartup(EntityUid uid, NPCMeleeCombatComponent component, ComponentStartup args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, true, combatMode);
        }
    }

    private void UpdateMelee(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<NPCMeleeCombatComponent, ActiveNPCComponent>();

        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (!_combatQuery.TryGetComponent(uid, out var combat) || !combat.IsInCombatMode)
            {
                RemComp<NPCMeleeCombatComponent>(uid);
                continue;
            }

            Attack(uid, comp, curTime);
        }
    }

    private void Attack(EntityUid uid, NPCMeleeCombatComponent component, TimeSpan curTime)
    {
        component.Status = CombatStatus.Normal;

        if (!_melee.TryGetWeapon(uid, out var weaponUid, out var weapon))
        {
            component.Status = CombatStatus.NoWeapon;
            return;
        }

        if (!TryComp(uid, out TransformComponent? xform) ||
            !TryComp(component.Target, out TransformComponent? targetXform))
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (!xform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (distance > TargetMeleeLostRange)
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        if (TryComp<NPCSteeringComponent>(uid, out var steering) &&
            steering.Status == SteeringStatus.NoPath)
        {
            component.Status = CombatStatus.TargetUnreachable;
            return;
        }

        // TODO: When I get parallel operators move this as NPC combat shouldn't be handling this.
        _steering.Register(uid, new EntityCoordinates(component.Target, Vector2.Zero), steering);

        if (distance > weapon.Range)
        {
            component.Status = CombatStatus.TargetOutOfRange;
            return;
        }

        if (weapon.NextAttack > curTime || !Enabled)
            return;

        if (_random.Prob(component.MissChance) &&
            _physicsQuery.TryGetComponent(component.Target, out var targetPhysics) &&
            targetPhysics.LinearVelocity.LengthSquared() != 0f)
        {
            _melee.AttemptLightAttackMiss(uid, weaponUid, weapon, targetXform.Coordinates.Offset(_random.NextVector2(0.5f)));
        }
        else
        {
            _melee.AttemptLightAttack(uid, weaponUid, weapon, component.Target);
        }
    }
}
