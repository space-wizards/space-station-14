namespace Content.Server.Roles
{
    public sealed class RoleAddedEvent : RoleEvent
    {
        public RoleAddedEvent(Mind.Mind mind, Role role) : base(mind, role) { }
    }
}
