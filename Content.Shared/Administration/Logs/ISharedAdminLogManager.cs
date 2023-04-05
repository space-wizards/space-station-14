using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

public interface ISharedAdminLogManager
{
    void Add(LogType type, LogImpact impact, ref LogStringHandler handler);

    void Add(LogType type, ref LogStringHandler handler);
}
