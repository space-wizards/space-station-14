namespace Content.Server.Database
{
    public sealed class ServerRoleTimerDef
    {
        public int Id { get; }
        public Guid UserId { get; }
        public string Role { get; }
        public TimeSpan TimeSpent { get; }

        public ServerRoleTimerDef(int id, Guid userId, string role, TimeSpan timeSpent)
        {
            Id = id;
            UserId = userId;
            Role = role;
            TimeSpent = timeSpent;
        }
    }
}
