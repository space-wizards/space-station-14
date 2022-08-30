using Content.Server.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Random;

namespace Content.Server.Physics.Controllers
{
    internal sealed class SingularityController : VirtualController
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        // SS13 has 10s but that's quite a while
        private const float MaxMoveCooldown = 5f;
        private const float MinMoveCooldown = 2f;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var (singularity, physics) in EntityManager.EntityQuery<ServerSingularityComponent, PhysicsComponent>())
            {
                if (EntityManager.HasComponent<ActorComponent>(singularity.Owner) ||
                    singularity.BeingDeletedByAnotherSingularity) continue;

                singularity.MoveAccumulator -= frameTime;

                if (singularity.MoveAccumulator > 0f) continue;

                singularity.MoveAccumulator = _robustRandom.NextFloat(MinMoveCooldown, MaxMoveCooldown);

                MoveSingulo(singularity, physics);
            }
        }

        private void MoveSingulo(ServerSingularityComponent singularity, PhysicsComponent physics)
        {
            // TODO: Need to make this events instead.
            if (singularity.Level <= 1)
            {
                physics.BodyStatus = BodyStatus.OnGround;
                return;
            }

            // TODO: Could try gradual changes instead
            var pushAngle = _robustRandom.NextAngle();
            var pushStrength = _robustRandom.NextFloat(0.75f, 1.0f);

            physics.LinearVelocity = Vector2.Zero;
            physics.BodyStatus = BodyStatus.InAir;
            physics.ApplyLinearImpulse(pushAngle.ToVec() * (pushStrength + 10f / Math.Min(singularity.Level, 4) * physics.Mass));
            // TODO: Speedcap it probably?
        }
    }
}
