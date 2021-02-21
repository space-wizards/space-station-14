using Content.Server.GameObjects.Components.Singularity;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Physics.Controllers
{
    internal sealed class SingularityController : AetherController
    {
        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);
            foreach (var singularity in ComponentManager.EntityQuery<SingularityComponent>())
            {
                if (singularity.Owner.HasComponent<BasicActorComponent>()) continue;

                // TODO: Need to essentially use a push vector in a random direction for us PLUS
                // Any entity colliding with our larger circlebox needs to have an impulse applied to itself.
            }
        }
    }
}
