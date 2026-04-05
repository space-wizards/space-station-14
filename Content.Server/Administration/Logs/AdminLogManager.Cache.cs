using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    /// <summary>
    /// V2 logging uses keyset pagination and participant-based queries against the database.
    /// The in-memory cache is disabled: <see cref="TrySearchCache"/> always returns false,
    /// so populating it wastes ~30k allocations per round for data that is never read.
    /// These methods are retained as no-ops to satisfy call sites in <see cref="SaveLogs"/>
    /// and <see cref="RoundStarting"/>.
    /// </summary>
    /// <remarks>
    /// Im pretty sure we dont need a cache because its faster now. Idk we will see
    /// </remarks>
    public void CacheNewRound()
    {
        // No-op: V2 cache disabled.
    }

    private void CacheLog(AdminLogEventWriteData log)
    {
        // No-op: V2 cache disabled.
    }

    private void CacheLog(SharedAdminLog log)
    {
        // No-op: V2 cache disabled.
    }

    private bool TrySearchCache(LogFilter? filter, [NotNullWhen(true)] out List<SharedAdminLog>? results)
    {
        // V2 is keyset + participant first — old cache filtering is obsolete.
        results = null;
        return false;
    }
}
