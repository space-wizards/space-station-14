using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class MovementBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Movement;

        public void Life(IEntity onEntity, OrganState state)
        {
            //TODO: Hook MoverComponent?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
