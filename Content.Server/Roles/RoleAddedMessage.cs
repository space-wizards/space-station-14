namespace Content.Server.Roles
{
    public class RoleAddedMessage : RoleMessage
    {
        public RoleAddedMessage(Role role) : base(role) { }
    }
}
