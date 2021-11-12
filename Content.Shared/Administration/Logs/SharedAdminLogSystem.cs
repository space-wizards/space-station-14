using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Shared.Administration.Logs;

public class SharedAdminLogSystem : EntitySystem
{
    public virtual void Add<T>(T log) where T : notnull
    {
        // noop
    }

    public virtual void Add<T>(T log, Guid playerId) where T : notnull
    {
        // noop
    }

    public virtual void Add<T>(T log, params Guid[] playerIds) where T : notnull
    {
        // noop
    }

    public virtual void Add<T>(T log, List<Guid> players) where T : notnull
    {
        // noop
    }
}
