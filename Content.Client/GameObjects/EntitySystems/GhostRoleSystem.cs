using Content.Shared.GameObjects.EntitySystems;

namespace Content.Client.GameObjects.EntitySystems
{
    public class GhostRoleSystem : SharedGhostRoleSystem
    {
        public delegate void OnRolesUpdated();
    }
}
