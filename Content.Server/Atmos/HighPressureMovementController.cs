#nullable enable
using System;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Server.Atmos
{
    public class HighPressureMovementController : FrictionController
    {
        [Dependency] private IRobustRandom _robustRandom = default!;
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        private const float MoveForcePushRatio = 1f;
        private const float MoveForceForcePushRatio = 1f;
        private const float ProbabilityOffset = 25f;
        private const float ProbabilityBasePercent = 10f;
        private const float ThrowForce = 100f;

        public void ExperiencePressureDifference(int cycle, float pressureDifference, AtmosDirection direction,
            float pressureResistanceProbDelta, EntityCoordinates throwTarget)
        {
            if (ControlledComponent == null)
                return;

            // TODO ATMOS stuns?

            var transform = ControlledComponent.Owner.Transform;
            var pressureComponent = ControlledComponent.Owner.GetComponent<MovedByPressureComponent>();
            var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
            var moveProb = 100f;

            if (pressureComponent.PressureResistance > 0)
                moveProb = MathF.Abs((pressureDifference / pressureComponent.PressureResistance * ProbabilityBasePercent) -
                           ProbabilityOffset);

            if (moveProb > ProbabilityOffset && _robustRandom.Prob(MathF.Min(moveProb / 100f, 1f))
                                             && !float.IsPositiveInfinity(pressureComponent.MoveResist)
                                             && (!ControlledComponent.Anchored
                                                 && (maxForce >= (pressureComponent.MoveResist * MoveForcePushRatio)))
                || (ControlledComponent.Anchored && (maxForce >= (pressureComponent.MoveResist * MoveForceForcePushRatio))))
            {


                if (maxForce > ThrowForce)
                {
                    if (throwTarget != EntityCoordinates.Invalid)
                    {
                        var moveForce = maxForce * MathHelper.Clamp(moveProb, 0, 100) / 150f;
                        var pos = ((throwTarget.Position - transform.Coordinates.Position).Normalized + direction.ToDirection().ToVec()).Normalized;
                        LinearVelocity = pos * moveForce;
                    }

                    else
                    {
                        var moveForce = MathF.Min(maxForce * MathHelper.Clamp(moveProb, 0, 100) / 2500f, 20f);
                        LinearVelocity = direction.ToDirection().ToVec() * moveForce;
                    }

                    pressureComponent.LastHighPressureMovementAirCycle = cycle;
                }
            }
        }
    }
}
