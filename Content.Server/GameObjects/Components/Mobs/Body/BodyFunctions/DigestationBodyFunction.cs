using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class DigestationBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Digestation;

        public void Life(IEntity onEntitty, OrganState state)
        {
            //TODO: Handle toxins maybe?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
