using Content.Server.GameObjects.Components.Observer;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GhostRoleSystem : EntitySystem
    {
        public GhostRoleSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(AvailableRoleComponent));
        }
    }
}
