using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class BloodPumpBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.BloodPump;

        public void Life(IEntity onEntitty, OrganState state)
        {
            //TODO: UGH
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
