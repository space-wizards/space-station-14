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

    private Task<Server>? _serverEntityTask;

    /// <summary>
    /// The entity that represents this server in the database.
    /// </summary>
    /// <remarks>
    /// This value is cached when first requested. Do not re-use this entity; if you need data like the rounds,
    /// request it manually with <see cref="IServerDbManager.AddOrGetServer"/>.
    /// </remarks>
    public Task<Server> ServerEntity => _serverEntityTask ??= GetServerEntity();

    private async Task<Server> GetServerEntity()
    {
        var name = _cfg.GetCVar(CCVars.AdminLogsServerName);
        var server = await _db.AddOrGetServer(name);

        _logManager.GetSawmill("db").Verbose("Server name: {Name}, ID in database: {Id}", server, server.Id);
        return server;
    }
}
