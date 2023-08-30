using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

[Virtual]
public class SharedAdminLogManager : ISharedAdminLogManager
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
