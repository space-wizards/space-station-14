#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics.Controllers;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;

namespace Content.Client.Physics.Controllers
{
    public sealed class MobMoverController : SharedMobMoverController
    {
        public override void UpdateBeforeSolve(float frameTime)
        {
            base.UpdateBeforeSolve(frameTime);

            var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
            if (player != null && player.TryGetComponent(out IPhysicsComponent? physicsComponent))
            {
                physicsComponent.Predict = true;
            }

            foreach (var (mover, physics) in ComponentManager.EntityQuery<SharedPlayerInputMoverComponent, PhysicsComponent>(false))
            {
                UpdateKinematics(frameTime, mover.Owner.Transform, mover, physics);
            }
        }
    }
}
