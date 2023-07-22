using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;
using Content.Server.Weapons.Melee;
using Content.Shared.NPC;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCJukeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityQuery<NPCMeleeCombatComponent> _npcMeleeQuery;
    private EntityQuery<NPCRangedCombatComponent> _npcRangedQuery;

    public override void Initialize()
    {
        base.Initialize();
        _npcMeleeQuery = GetEntityQuery<NPCMeleeCombatComponent>();
        _npcRangedQuery = GetEntityQuery<NPCRangedCombatComponent>();

        SubscribeLocalEvent<NPCJukeComponent, NPCSteeringEvent>(OnJukeSteering);
    }

    private void OnJukeSteering(EntityUid uid, NPCJukeComponent component, ref NPCSteeringEvent args)
    {
        if (_npcMeleeQuery.TryGetComponent(uid, out var melee))
        {
            switch (component.JukeType)
            {
                case JukeType.Away:
                    if (!TryComp<MeleeWeaponComponent>(melee.Weapon, out var weapon))
                        return;

                    var cdRemaining = weapon.NextAttack - _timing.CurTime;
                    var attackCooldown = TimeSpan.FromSeconds(1f / _melee.GetAttackRate(melee.Weapon, uid, weapon));

                    // Might as well get in range.
                    if (cdRemaining < attackCooldown * 0.45f)
                        return;

                    if (!_physics.TryGetNearestPoints(uid, melee.Target, out var pointA, out var pointB))
                        return;

                    var obstacleDirection = pointB - args.WorldPosition;

                    // If they're moving away then pursue anyway.
                    // If just hit then always back up a bit.
                    if (cdRemaining < attackCooldown * 0.90f &&
                        TryComp<PhysicsComponent>(melee.Target, out var targetPhysics) &&
                        Vector2.Dot(targetPhysics.LinearVelocity, obstacleDirection) > 0f)
                    {
                        return;
                    }

                    if (cdRemaining < TimeSpan.FromSeconds(1f / _melee.GetAttackRate(melee.Weapon, uid, weapon)) * 0.45f)
                        return;

                    var idealDistance = weapon.Range * 4f;
                    var obstacleDistance = obstacleDirection.Length();

                    if (obstacleDistance > idealDistance || obstacleDistance == 0f)
                    {
                        // Don't want to get too far.
                        return;
                    }

                    obstacleDirection = args.OffsetRotation.RotateVec(obstacleDirection);
                    var norm = obstacleDirection.Normalized();

                    var weight = obstacleDistance <= args.Steering.Radius
                        ? 1f
                        : (idealDistance - obstacleDistance) / idealDistance;

                    for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
                    {
                        var result = -Vector2.Dot(norm, NPCSteeringSystem.Directions[i]) * weight;

                        if (result < 0f)
                            continue;

                        args.Steering.Interest[i] = MathF.Max(args.Steering.Interest[i], result);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            args.Steering.CanSeek = false;
            return;
        }

        // TODO: Just use some cooldown
        if (_npcRangedQuery.TryGetComponent(uid, out var ranged))
        {

        }
    }
}
