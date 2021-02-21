#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics.Controllers;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Client.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);

            var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
            if (player == null ||
                !player.TryGetComponent(out IMoverComponent? mover) ||
                !player.TryGetComponent(out PhysicsComponent? body)) return;

            body.Predict = true; // TODO: equal prediction instead of true?

            // Server-side should just be handled on its own so we'll just do this shizznit
            if (player.TryGetComponent(out IMobMoverComponent? mobMover))
            {
                HandleMobMovement(mover, body, mobMover);
                return;
            }

            HandleKinematicMovement(mover, body);
        }
    }
}
