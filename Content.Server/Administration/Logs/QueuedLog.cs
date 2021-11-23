using System.Collections.Generic;
using Content.Server.Database;
using JetBrains.Annotations;

namespace Content.Server.Administration.Logs;

public readonly struct QueuedLog
{
    public QueuedLog(AdminLog log, List<(int id, string? name)> entities)
    {
        Log = log;
        Entities = entities;
    }

    public AdminLog Log { get; }

    public List<(int id, string? name)> Entities { get; }

    public void Deconstruct(out AdminLog log, out List<(int id, string? name)> entities)
    {
        log = Log;
        entities = Entities;
    }
}
