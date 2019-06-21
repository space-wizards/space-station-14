using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class ManipulationBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Manipulation;

        public void Life(IEntity onEntitty, OrganState state)
        {
            //TODO: Hook hands component maybe?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
