using Robust.Shared.GameObjects;

namespace Content.Server.Mobs.Roles
{
    public class RoleMessage : ComponentMessage
    {
        public readonly Role Role;

        public RoleMessage(Role role)
        {
            Role = role;
        }
    }
}
