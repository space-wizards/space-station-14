using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Shared.CombatMode;
using Content.Shared.NPC;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    private const float TargetMeleeLostRange = 14f;

    private void InitializeMelee()
    {
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentStartup>(OnMeleeStartup);
        SubscribeLocalEvent<NPCMeleeCombatComponent, ComponentShutdown>(OnMeleeShutdown);
        SubscribeLocalEvent<NPCMeleeCombatComponent, NPCSteeringEvent>(OnMeleeSteering);
    }

    private void OnMeleeSteering(EntityUid uid, NPCMeleeCombatComponent component, ref NPCSteeringEvent args)
    {
        args.Steering.CanSeek = true;

        if (TryComp<MeleeWeaponComponent>(component.Weapon, out var weapon))
        {
            var cdRemaining = weapon.NextAttack - _timing.CurTime;

            // If CD remaining then backup.
            if (cdRemaining < TimeSpan.FromSeconds(1f / weapon.AttackRate) * 0.5f)
                return;

            if (!_physics.TryGetNearestPoints(uid, component.Target, out var pointA, out var pointB))
                return;

            var idealDistance = weapon.Range * 1.5f;
            var obstacleDirection = pointB - args.WorldPosition;
            var obstacleDistance = obstacleDirection.Length;

            if (obstacleDistance > idealDistance || obstacleDistance == 0f)
            {
                // Don't want to get too far.
                return;
            }

            args.Steering.CanSeek = false;
            obstacleDirection = args.OffsetRotation.RotateVec(obstacleDirection);
            var norm = obstacleDirection.Normalized;

            var weight = (obstacleDistance <= args.AgentRadius
                ? 1f
                : (idealDistance - obstacleDistance) / idealDistance);

            for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
            {
                var result = -Vector2.Dot(norm, NPCSteeringSystem.Directions[i]) * weight;

                if (result < 0f)
                    continue;

                args.Interest[i] = MathF.Max(args.Interest[i], result);
            }
        }
    }

    private void OnMeleeShutdown(EntityUid uid, NPCMeleeCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, false, combatMode);
        }

        _steering.Unregister(component.Owner);
    }

    private void OnMeleeStartup(EntityUid uid, NPCMeleeCombatComponent component, ComponentStartup args)
    {
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, true, combatMode);
        }

        // TODO: Cleanup later, just looking for parity for now.
        component.Weapon = uid;
    }

    private void UpdateMelee(float frameTime)
    {
        var combatQuery = GetEntityQuery<CombatModeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var curTime = _timing.CurTime;

        foreach (var (comp, _) in EntityQuery<NPCMeleeCombatComponent, ActiveNPCComponent>())
        {
            var uid = comp.Owner;

            if (!combatQuery.TryGetComponent(uid, out var combat) || !combat.IsInCombatMode)
            {
                RemComp<NPCMeleeCombatComponent>(uid);
                continue;
            }

            Attack(uid, comp, curTime, physicsQuery, xformQuery);
        }
    }

    private void Attack(EntityUid uid, NPCMeleeCombatComponent component, TimeSpan curTime, EntityQuery<PhysicsComponent> physicsQuery, EntityQuery<TransformComponent> xformQuery)
    {
        component.Status = CombatStatus.Normal;

        if (!TryComp<MeleeWeaponComponent>(component.Weapon, out var weapon))
        {
            component.Status = CombatStatus.NoWeapon;
            return;
        }

        if (!xformQuery.TryGetComponent(uid, out var xform) ||
            !xformQuery.TryGetComponent(component.Target, out var targetXform))
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

        steering = EnsureComp<NPCSteeringComponent>(uid);
        steering.Range = MathF.Max(0.2f, weapon.Range - 0.4f);

        // Gets unregistered on component shutdown.
        _steering.TryRegister(uid, new EntityCoordinates(component.Target, Vector2.Zero), steering);

        if (weapon.NextAttack > curTime || !Enabled)
            return;

        if (_random.Prob(component.MissChance) &&
            physicsQuery.TryGetComponent(component.Target, out var targetPhysics) &&
            targetPhysics.LinearVelocity.LengthSquared != 0f)
        {
            _melee.AttemptLightAttackMiss(uid, component.Weapon, weapon, targetXform.Coordinates.Offset(_random.NextVector2(0.5f)));
        }
        else
        {
            _melee.AttemptLightAttack(uid, component.Weapon, weapon, component.Target);
        }
    }
}
