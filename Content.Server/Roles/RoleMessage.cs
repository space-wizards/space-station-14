using Robust.Shared.GameObjects;

namespace Content.Server.Roles
{
#pragma warning disable 618
    public class RoleMessage : ComponentMessage
#pragma warning restore 618
    {
        public readonly Role Role;

        public RoleMessage(Role role)
        {
            Role = role;
        }
    }
}
