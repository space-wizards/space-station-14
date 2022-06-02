using Content.Server.Database;

namespace Content.Server.RoleTimer.Managers
{
    public sealed class RoleTimerManager
    {
        [Dependency] private readonly IServerDbManager _db = default!;
    }
}
