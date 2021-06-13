namespace Content.Server.Roles
{
    public class RoleRemovedMessage : RoleMessage
    {
        public RoleRemovedMessage(Role role) : base(role) { }
    }
}
