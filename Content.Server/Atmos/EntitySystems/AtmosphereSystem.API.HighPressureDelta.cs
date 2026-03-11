using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Applies the effects of a pressure difference to an entity with a <see cref="MovedByPressureComponent"/>.
    /// </summary>
    /// <param name="ent">The <see cref="Entity{T}"/> to apply the pressure difference to.</param>
    /// <param name="cycle">The current sim cycle.</param>
    /// <param name="pressureDifference">The pressure difference to apply.</param>
    /// <param name="direction">The direction of the pressure difference.</param>
    /// <param name="pressureResistanceProbDelta"></param>
    /// <param name="throwTarget">The target coordinates to throw the entity to.
    /// If invalid, the entity will be pushed in the direction of the pressure difference instead.</param>
    /// <param name="gridWorldRotation">The world rotation of the grid the entity is on,
    /// used to adjust the direction of the pressure difference.</param>
    /// <param name="xform">The entity's <see cref="TransformComponent"/>.</param>
    /// <param name="physics">The entity's <see cref="PhysicsComponent"/>.</param>
    [PublicAPI]
    public void ExperiencePressureDifference( // shouldnt really be an API but its public rn so we'll need to deprecate it later
        Entity<MovedByPressureComponent> ent,
        int cycle,
        float pressureDifference,
        AtmosDirection direction,
        float pressureResistanceProbDelta, // unused, deprecate soon
        EntityCoordinates throwTarget,
        Angle gridWorldRotation,
        TransformComponent? xform = null,
        PhysicsComponent? physics = null)
    {
        var (uid, component) = ent;
        if (!Resolve(uid, ref physics, false))
            return;

        if (!Resolve(uid, ref xform))
            return;

        // TODO ATMOS stuns?

        var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
        var moveProb = 100f;

        if (component.PressureResistance > 0)
        {
            moveProb = MathF.Abs((pressureDifference / component.PressureResistance * MovedByPressureComponent.ProbabilityBasePercent) -
                                 MovedByPressureComponent.ProbabilityOffset);
        }

        // Can we yeet the thing (due to probability, strength, etc.)
        if (moveProb > MovedByPressureComponent.ProbabilityOffset && _random.Prob(MathF.Min(moveProb / 100f, 1f))
                                                                  && !float.IsPositiveInfinity(component.MoveResist)
                                                                  && (physics.BodyType != BodyType.Static
                                                                      && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForcePushRatio)))
            || (physics.BodyType == BodyType.Static && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForceForcePushRatio))))
        {
            if (HasComp<MobStateComponent>(uid))
            {
                AddMobMovedByPressure(uid, component, physics);
            }

            if (maxForce > MovedByPressureComponent.ThrowForce)
            {
                var moveForce = maxForce;
                moveForce /= (throwTarget != EntityCoordinates.Invalid) ? SpaceWindPressureForceDivisorThrow : SpaceWindPressureForceDivisorPush;
                moveForce *= MathHelper.Clamp(moveProb, 0, 100);

                // Apply a sanity clamp to prevent being thrown through objects.
                var maxSafeForceForObject = SpaceWindMaxVelocity * physics.Mass;
                moveForce = MathF.Min(moveForce, maxSafeForceForObject);

                // Grid-rotation adjusted direction
                var dirVec = (direction.ToAngle() + gridWorldRotation).ToWorldVec();

                // TODO: Technically these directions won't be correct but uhh I'm just here for optimisations buddy not to fix my old bugs.
                if (throwTarget != EntityCoordinates.Invalid)
                {
                    var pos = ((_transformSystem.ToMapCoordinates(throwTarget).Position - _transformSystem.GetWorldPosition(xform)).Normalized() + dirVec).Normalized();
                    _physics.ApplyLinearImpulse(uid, pos * moveForce, body: physics);
                }
                else
                {
                    moveForce = MathF.Min(moveForce, SpaceWindMaxPushForce);
                    _physics.ApplyLinearImpulse(uid, dirVec * moveForce, body: physics);
                }

                component.LastHighPressureMovementAirCycle = cycle;
            }
        }
    }
}
