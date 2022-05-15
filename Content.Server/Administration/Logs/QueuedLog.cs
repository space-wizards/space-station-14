using Content.Server.Database;

namespace Content.Server.Administration.Logs;

public readonly struct QueuedLog
{
    public QueuedLog(AdminLog log, Dictionary<int, string?> entities)
    {
        Log = log;
        Entities = entities;
    }

    public AdminLog Log { get; }

    public Dictionary<int, string?> Entities { get; }

    public void Deconstruct(out AdminLog log, out Dictionary<int, string?> entities)
    {
        log = Log;
        entities = Entities;
    }
}
