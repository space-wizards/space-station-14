using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;
using Content.Shared.MobState.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "MovedByPressure";

        private const float MoveForcePushRatio = 1f;
        private const float MoveForceForcePushRatio = 1f;
        private const float ProbabilityOffset = 25f;
        private const float ProbabilityBasePercent = 10f;
        private const float ThrowForce = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressureResistance")]
        public float PressureResistance { get; set; } = 1f;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("moveResist")]
        public float MoveResist { get; set; } = 100f;
        [ViewVariables(VVAccess.ReadWrite)]
        public int LastHighPressureMovementAirCycle { get; set; } = 0;

        public void ExperiencePressureDifference(int cycle, float pressureDifference, AtmosDirection direction,
            float pressureResistanceProbDelta, EntityCoordinates throwTarget)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent? physics))
                return;

            // TODO ATMOS stuns?

            var transform = physics.Owner.Transform;
            var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
            var moveProb = 100f;

            if (PressureResistance > 0)
                moveProb = MathF.Abs((pressureDifference / PressureResistance * ProbabilityBasePercent) -
                           ProbabilityOffset);

            if (moveProb > ProbabilityOffset && _robustRandom.Prob(MathF.Min(moveProb / 100f, 1f))
                                             && !float.IsPositiveInfinity(MoveResist)
                                             && (physics.BodyType != BodyType.Static
                                                 && (maxForce >= (MoveResist * MoveForcePushRatio)))
                || (physics.BodyType == BodyType.Static && (maxForce >= (MoveResist * MoveForceForcePushRatio))))
            {
                if (physics.Owner.HasComponent<MobStateComponent>())
                {
                    physics.BodyStatus = BodyStatus.InAir;

                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionMask &= ~(int) CollisionGroup.VaultImpassable;
                    }

                    Owner.SpawnTimer(2000, () =>
                    {
                        if (Deleted || !Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

                        // Uhh if you get race conditions good luck buddy.
                        if (physicsComponent.Owner.HasComponent<MobStateComponent>())
                        {
                            physicsComponent.BodyStatus = BodyStatus.OnGround;
                        }

                        foreach (var fixture in physics.Fixtures)
                        {
                            fixture.CollisionMask |= (int) CollisionGroup.VaultImpassable;
                        }
                    });
                }

                if (maxForce > ThrowForce)
                {
                    // Vera please fix ;-;
                    if (throwTarget != EntityCoordinates.Invalid)
                    {
                        var moveForce = maxForce * MathHelper.Clamp(moveProb, 0, 100) / 15f;
                        var pos = ((throwTarget.Position - transform.Coordinates.Position).Normalized + direction.ToDirection().ToVec()).Normalized;
                        physics.ApplyLinearImpulse(pos * moveForce);
                    }

                    else
                    {
                        var moveForce = MathF.Min(maxForce * MathHelper.Clamp(moveProb, 0, 100) / 2500f, 20f);
                        physics.ApplyLinearImpulse(direction.ToDirection().ToVec() * moveForce);
                    }

                    LastHighPressureMovementAirCycle = cycle;
                }
            }
        }
    }

    public static class MovedByPressureExtensions
    {
        public static bool IsMovedByPressure(this IEntity entity)
        {
            return entity.IsMovedByPressure(out _);
        }

        public static bool IsMovedByPressure(this IEntity entity, [NotNullWhen(true)] out MovedByPressureComponent? moved)
        {
            return entity.TryGetComponent(out moved) &&
                   moved.Enabled;
        }
    }
}
