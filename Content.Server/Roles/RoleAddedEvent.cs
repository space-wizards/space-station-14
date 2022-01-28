namespace Content.Server.Roles
{
    public sealed class RoleAddedEvent : RoleEvent
    {
        public RoleAddedEvent(Role role) : base(role) { }
    }
}
