namespace Content.Server.Roles
{
    public sealed class RoleRemovedEvent : RoleEvent
    {
        public RoleRemovedEvent(Mind.Mind mind, Role role) : base(mind, role) { }
    }
}
