using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class SightBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Vision;

        public void Life(IEntity onEntity, OrganState state)
        {
            //TODO: Hook Eye?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
