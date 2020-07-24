using NFluidsynth;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Logger = Robust.Shared.Log.Logger;
using MathF = CannyFastMath.MathF;

namespace Content.Server.Atmos
{
    public class HighPressureMovementController : VirtualController
    {
        [Dependency] private IRobustRandom _robustRandom = default!;
        public override ICollidableComponent? ControlledComponent { protected get; set; }

        private const float MoveForcePushRatio = 1f;
        private const float MoveForceForcePushRatio = 1f;
        private const float ProbabilityOffset = 25f;
        private const float ProbabilityBasePercent = 10f;
        private const float ThrowForce = 4000f;

        public void ExperiencePressureDifference(int cycle, float pressureDifference, Direction direction,
            float pressureResistanceProbDelta, TileAtmosphere throwTarget)
        {
            if (ControlledComponent == null)
                return;

            // TODO ATMOS stuns?

            var pressureComponent = ControlledComponent.Owner.GetComponent<MovedByPressureComponent>();
            var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
            var moveProb = 100f;

            if (pressureComponent.PressureResistance > 0)
                moveProb = MathF.Abs((pressureDifference / pressureComponent.PressureResistance * ProbabilityBasePercent) -
                           ProbabilityOffset);

            if(pressureDifference > 10f)
                Logger.Info($"PRESS DIFF! PROB: {moveProb/100f} FORCE: {maxForce} DIFF: {pressureDifference}");

            if (moveProb > ProbabilityOffset && _robustRandom.Prob(MathF.Min(moveProb / 100f, 1f))
                                             && !float.IsPositiveInfinity(pressureComponent.MoveResist)
                                             && (!ControlledComponent.Anchored
                                                 && (maxForce >= (pressureComponent.MoveResist * MoveForcePushRatio)))
                || (ControlledComponent.Anchored && (maxForce >= (pressureComponent.MoveResist * MoveForceForcePushRatio))))
            {
                var moveForce = MathF.Min(maxForce * MathF.Clamp(moveProb, 0, 100) / 100f, 25f);
                LinearVelocity = direction.ToVec() * moveForce;

                Logger.Info($"MOVED! {moveForce} {LinearVelocity}");

                pressureComponent.LastHighPressureMovementAirCycle = cycle;
            }
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();
            LinearVelocity *= 0.85f;
            if (LinearVelocity.Length < 1f)
                Stop();
        }
    }
}
