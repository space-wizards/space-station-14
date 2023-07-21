using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;
using Content.Server.Weapons.Melee;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
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
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<NPCMeleeCombatComponent> _meleeQuery;
    private EntityQuery<NPCRangedCombatComponent> _rangedQuery;

    public override void Initialize()
    {
        base.Initialize();
        _meleeQuery = GetEntityQuery<NPCMeleeCombatComponent>();
        _rangedQuery = GetEntityQuery<NPCRangedCombatComponent>();

        SubscribeLocalEvent<NPCJukeComponent, NPCSteeringEvent>(OnMeleeSteering);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NPCJukeComponent, NPCSteeringComponent, InputMoverComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var juke, out var steering, out var mover, out var xform))
        {
            var result = TryJuke(uid, juke, steering, mover, xform);
            juke.Juking = result;
        }
    }

    private bool TryJuke(EntityUid uid, NPCJukeComponent juke, NPCSteeringComponent steering, InputMoverComponent mover, TransformComponent xform)
    {
        if (_meleeQuery.TryGetComponent(uid, out var melee))
        {
            var worldPosition = _transform.GetWorldPosition(xform);
            var offsetRot = -_mover.GetParentGridAngle(mover);

            switch (juke.JukeType)
            {
                case JukeType.Away:
                    if (!TryComp<MeleeWeaponComponent>(melee.Weapon, out var weapon))
                        return false;

                    var cdRemaining = weapon.NextAttack - _timing.CurTime;
                    var attackCooldown = TimeSpan.FromSeconds(1f / _melee.GetAttackRate(melee.Weapon, uid, weapon));

                    // Might as well get in range.
                    if (cdRemaining < attackCooldown * 0.45f)
                        return false;

                    if (!_physics.TryGetNearestPoints(uid, melee.Target, out var pointA, out var pointB))
                        return false;

                    var obstacleDirection = pointB - worldPosition;

                    // If they're moving away then pursue anyway.
                    // If just hit then always back up a bit.
                    if (cdRemaining < attackCooldown * 0.90f &&
                        TryComp<PhysicsComponent>(melee.Target, out var targetPhysics) &&
                        Vector2.Dot(targetPhysics.LinearVelocity, obstacleDirection) > 0f)
                    {
                        return false;
                    }

                    if (cdRemaining < TimeSpan.FromSeconds(1f / _melee.GetAttackRate(melee.Weapon, uid, weapon)) * 0.45f)
                        return false;

                    var idealDistance = weapon.Range * 4f;
                    var obstacleDistance = obstacleDirection.Length();

                    if (obstacleDistance > idealDistance || obstacleDistance == 0f)
                    {
                        // Don't want to get too far.
                        return false;
                    }

                    obstacleDirection = offsetRot.RotateVec(obstacleDirection);
                    var norm = obstacleDirection.Normalized();

                    var weight = obstacleDistance <= steering.Radius
                        ? 1f
                        : (idealDistance - obstacleDistance) / idealDistance;

                    for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
                    {
                        var result = -Vector2.Dot(norm, NPCSteeringSystem.Directions[i]) * weight;

                        if (result < 0f)
                            continue;

                        steering.Interest[i] = MathF.Max(steering.Interest[i], result);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        // TODO: Just use some cooldown
        if (_rangedQuery.TryGetComponent(uid, out var ranged))
        {

        }

        return false;
    }

    private void OnMeleeSteering(EntityUid uid, NPCJukeComponent component, ref NPCSteeringEvent args)
    {
        if (!component.Juking)
            return;

        args.Steering.CanSeek = false;
    }
}
