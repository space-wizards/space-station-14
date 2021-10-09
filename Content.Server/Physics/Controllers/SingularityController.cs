using Content.Server.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Random;

namespace Content.Server.Physics.Controllers
{
    internal sealed class SingularityController : VirtualController
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private const float MaxMoveCooldown = 10f;
        private const float MinMoveCooldown = 2f;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var (singularity, physics) in EntityManager.EntityQuery<ServerSingularityComponent, PhysicsComponent>())
            {
                if (singularity.Owner.HasComponent<ActorComponent>() ||
                    singularity.BeingDeletedByAnotherSingularity) continue;

                singularity.MoveAccumulator -= frameTime;

                if (singularity.MoveAccumulator > 0f) continue;

                singularity.MoveAccumulator = MinMoveCooldown + (MaxMoveCooldown - MinMoveCooldown) * _robustRandom.NextFloat();

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
            var pushVector = new Vector2(_robustRandom.Next(-10, 10), _robustRandom.Next(-10, 10));

            if (pushVector == Vector2.Zero) return;

            physics.LinearVelocity = Vector2.Zero;
            physics.BodyStatus = BodyStatus.InAir;
            physics.ApplyLinearImpulse(pushVector.Normalized + 1f / singularity.Level * physics.Mass);
            // TODO: Speedcap it probably?
        }
    }
}
