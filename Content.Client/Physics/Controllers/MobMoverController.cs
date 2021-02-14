#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics.Controllers;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Client.Physics.Controllers
{
    public sealed class MobMoverController : SharedMobMoverController
    {
        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);

            var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
            if (player == null ||
                !player.TryGetComponent(out SharedPlayerInputMoverComponent? mover) ||
                !player.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            physicsComponent.Predict = true; // TODO: equal prediction instead of true?
            UpdateKinematics(frameTime, player.Transform, mover, physicsComponent);
        }
    }
}
