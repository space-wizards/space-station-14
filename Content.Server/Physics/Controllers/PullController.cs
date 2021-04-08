#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public class PullController : VirtualController
    {
        private const int ImpulseModifier = 20;

        private SharedPullingSystem _pullableSystem = default!;

        public override List<Type> UpdatesAfter => new() {typeof(MoverController)};

        public override void Initialize()
        {
            base.Initialize();

            _pullableSystem = EntitySystem.Get<SharedPullingSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var pullable in _pullableSystem.Moving)
            {
                if (pullable.Deleted)
                {
                    continue;
                }

                if (pullable.MovingTo == null)
                {
                    continue;
                }

                DebugTools.AssertNotNull(pullable.Puller);

                var pullerPosition = pullable.Puller!.Transform.MapPosition;
                if (pullable.MovingTo.Value.MapId != pullerPosition.MapId)
                {
                    pullable.MovingTo = null;
                    continue;
                }

                if (!pullable.Owner.TryGetComponent<PhysicsComponent>(out var physics) ||
                    physics.BodyType == BodyType.Static ||
                    pullable.MovingTo.Value.MapId != pullable.Owner.Transform.MapID)
                {
                    pullable.MovingTo = null;
                    continue;
                }

                var movingPosition = pullable.MovingTo.Value.Position;
                var ownerPosition = pullable.Owner.Transform.MapPosition.Position;

                if (movingPosition.EqualsApprox(ownerPosition, 0.01))
                {
                    pullable.MovingTo = null;
                    continue;
                }

                var diff = movingPosition - ownerPosition;
                var diffLength = diff.Length;
                var multiplier = diffLength < 1 ? ImpulseModifier * diffLength : ImpulseModifier;
                var impulse = diff.Normalized * multiplier;

                physics.ApplyLinearImpulse(impulse);
            }
        }
    }
}
