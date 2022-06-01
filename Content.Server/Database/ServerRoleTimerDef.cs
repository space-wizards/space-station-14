namespace Content.Server.Database
{
    public sealed class ServerRoleTimerDef
    {
        /// <summary>
        /// The player that this role timer belongs to.
        /// </summary>
        public Guid UserId { get; }
        /// <summary>
        /// The job (or role) that this timer is tracking.
        /// </summary>
        public string Role { get; }
        /// <summary>
        /// Time spent playing this role.
        /// </summary>
        public TimeSpan TimeSpent { get; }

        public ServerRoleTimerDef(Guid userId, string role, TimeSpan timeSpent)
        {
            UserId = userId;
            Role = role;
            TimeSpent = timeSpent;
        }
    }
}
