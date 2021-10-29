using Robust.Shared.GameObjects;

namespace Content.Server.Roles
{
    public class RoleEvent : EntityEventArgs
    {
        public readonly Role Role;

        public RoleEvent(Role role)
        {
            Role = role;
        }
    }
}
