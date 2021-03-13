using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Pulling;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    public class PullController : VirtualController
    {
        public override List<Type> UpdatesAfter => new() {typeof(MoverController)};

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var pullable in ComponentManager.EntityQuery<SharedPullableComponent>())
            {
                if (pullable.MovingTo == null)
                {
                    continue;
                }

                if (!pullable.Owner.TryGetComponent<PhysicsComponent>(out var physics) ||
                    physics.BodyType == BodyType.Static)
                {
                    pullable.MovingTo = null;
                    continue;
                }

                var diff = pullable.MovingTo.Value.Position - pullable.Owner.Transform.Coordinates.Position;
                physics.ApplyLinearImpulse(diff.Normalized * 20);
            }
        }
    }
}
