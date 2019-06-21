using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class BreathingBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Breathing;

        public void Life(IEntity onEntitty, OrganState state)
        {
            //TODO: Hook something later?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
