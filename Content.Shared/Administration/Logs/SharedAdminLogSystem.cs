using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Shared.Administration.Logs;

public class SharedAdminLogSystem : EntitySystem
{
    public virtual void Add<T>(T log)
    {
        // noop
    }

    public virtual void Add<T>(T log, Guid playerId)
    {
        // noop
    }

    public virtual void Add<T>(T log, List<Guid> players)
    {
        // noop
    }
}
