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
public sealed partial class ServerDbEntryManager
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ILogManager _logManager = default!;

    private const int MaxRetries = 5;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(2);

    private Task<Server>? _serverEntityTask;
    private int _consecutiveFailures;

    /// <summary>
    /// The entity that represents this server in the database.
    /// </summary>
    /// <remarks>
    /// This value is cached when first requested. Do not re-use this entity; if you need data like the rounds,
    /// request it manually with <see cref="IServerDbManager.AddOrGetServer"/>.
    /// </remarks>
    /// <summary>
    /// Returns the cached server entity task, or starts a new one if the previous attempt failed.
    /// </summary>
    public Task<Server> ServerEntity
    {
        get
        {
            if (_serverEntityTask is { IsCompletedSuccessfully: true })
                return _serverEntityTask;

            return _serverEntityTask = GetServerEntityWithRetry();
        }
    }

    private async Task<Server> GetServerEntityWithRetry()
    {
        var sawmill = _logManager.GetSawmill("db");
        var delay = InitialRetryDelay;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var name = _cfg.GetCVar(CCVars.AdminLogsServerName);
                var server = await _db.AddOrGetServer(name);
                sawmill.Verbose("Server name: {Name}, ID in database: {Id}", server, server.Id);
                Interlocked.Exchange(ref _consecutiveFailures, 0);
                return server;
            }
            catch (Exception e)
            {
                var failures = Interlocked.Increment(ref _consecutiveFailures);

                if (attempt >= MaxRetries - 1)
                {
                    sawmill.Error($"Failed to resolve server identity after {MaxRetries} attempts. Last error: {e}");
                    throw;
                }

                sawmill.Warning($"Failed to resolve server identity (attempt {attempt + 1}/{MaxRetries}, total failures: {failures}): {e.Message}");
                await Task.Delay(delay);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxRetryDelay.Ticks));
            }
        }
    }
}
