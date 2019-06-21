using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class ConsciousnessBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Consciousness;

        public void Life(IEntity onEntitty, OrganState state)
        {
            //TODO: Hook MindComponent?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
