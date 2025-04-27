using System.Diagnostics.CodeAnalysis;
using Content.Server.Database;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerPlayTimeManager
{
    void Initialize();
    bool UsePlayTimeServer();
    bool SaveLocaly();
    Task<List<PlayTime>> GetPlayTimesAsync(Guid playerId, CancellationToken cancel);
    Task UpdatePlayTimes(IEnumerable<PlayTime> updates);
}
