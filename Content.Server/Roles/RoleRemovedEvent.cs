namespace Content.Server.Roles
{
    public sealed class RoleRemovedEvent : RoleEvent
    {
        public RoleRemovedEvent(Role role) : base(role) { }
    }
}
