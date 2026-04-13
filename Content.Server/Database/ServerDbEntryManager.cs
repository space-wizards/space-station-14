using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Database;

/// <summary>
/// Stupid tiny manager whose sole purpose is keeping track of the <see cref="Server"/> database entry for this server.
/// </summary>
/// <remarks>
/// This allows the value to be cached,
/// so it can be easily retrieved by later code that needs to log the server ID to the database.
/// </remarks>
public sealed class ServerDbEntryManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private Server? _cachedServer;

    /// <summary>
    /// Returns the cached server entity, resolving it from the DB on first access.
    /// After the first successful resolution, this always returns a completed task.
    /// </summary>
    public Task<Server> ServerEntity
    {
        get
        {
            if (_cachedServer != null)
                return Task.FromResult(_cachedServer);

            return ResolveAndCache();
        }
    }

    private async Task<Server> ResolveAndCache()
    {
        var sawmill = _logManager.GetSawmill("db");

        try
        {
            var name = _cfg.GetCVar(CCVars.AdminLogsServerName);
            var server = await _db.AddOrGetServer(name);
            sawmill.Verbose("Server name: {Name}, ID in database: {Id}", server, server.Id);
            _cachedServer = server;
            return server;
        }
        catch (Exception e)
        {
            sawmill.Error($"Failed to resolve server identity: {e}");
            throw;
        }
    }
}
