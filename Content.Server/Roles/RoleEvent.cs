using Robust.Shared.GameObjects;

namespace Content.Server.Roles
{
    public class RoleEvent : EntityEventArgs
    {
        public readonly Mind.Mind Mind;
        public readonly Role Role;

        public RoleEvent(Mind.Mind mind, Role role)
        {
            Mind = mind;
            Role = role;
        }
    }
}
