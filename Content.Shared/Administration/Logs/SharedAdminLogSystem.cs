using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

public abstract class SharedAdminLogSystem : EntitySystem
{
    public virtual void Add(LogType type, LogImpact impact, ref LogStringHandler handler)
    {
        // noop
    }

    public virtual void Add(LogType type, ref LogStringHandler handler)
    {
        // noop
    }
}
