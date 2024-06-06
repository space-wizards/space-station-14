using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Configuration;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Content.Server.Database;

/* TODO:
 * task run thread calling wait async on the connection to receive postgres notifications.
 * Handle notifications in the c# event on the connection
 */


public sealed class PostgresNotificationManager : IDisposable
{
    private const string BanNotificationChannel = "BanNotification";
    private const string PostgresDbEngine = "postgres";

    [Dependency] private readonly IConfigurationManager _cfg = default!;
#if EXCEPTION_TOLERANCE
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif
    // QUESTION: What, if at all, should be logged?
    //[Dependency] private readonly ISawmill _logger = default!;

    private NpgsqlConnection? _connection;

    public async Task InitAsync(CancellationToken cancellationToken)
    {
        if (!_cfg.GetCVar(CCVars.DatabaseEngine).Equals(PostgresDbEngine, StringComparison.CurrentCultureIgnoreCase))
            return;

        var host = _cfg.GetCVar(CCVars.DatabasePgHost);
        var port = _cfg.GetCVar(CCVars.DatabasePgPort);
        var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
        var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
        var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = db,
            Username = user,
            Password = pass
        };

        _connection = new NpgsqlConnection(builder.ConnectionString);
        _connection.Notification += OnNotification;
    }

    public void Listen(CancellationToken cancellationToken)
    {
        if (_connection == null)
            return;

        Task.Run(() => NotificationListener(cancellationToken), cancellationToken);
    }

    private async Task NotificationListener(CancellationToken cancellationToken)
    {
        if (_connection == null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            // QUESTION: ConnectionState.Broken apparently isn't used yet. Should this be removed?
            if (_connection.State == ConnectionState.Broken)
                await _connection.CloseAsync();

            // QUESTION: Is reconnection when the connection got closed even desired?
            if (_connection.State == ConnectionState.Closed)
            {
                await _connection.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand($"LISTEN {BanNotificationChannel}", _connection);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            // QUESTION: Exception tolerance?
#if EXCEPTION_TOLERANCE
            try
            {
#endif
                await _connection.WaitAsync(cancellationToken);
#if EXCEPTION_TOLERANCE
            }
            catch (NpgsqlException e)
            {
                _runtimeLog.LogException(e, $"{nameof(PostgresNotificationManager)}.${nameof(NotificationListener)}");
            }
#endif
        }
    }

    private void OnNotification(object _, NpgsqlNotificationEventArgs notification)
    {
        // TODO: Implement handling the notification
    }


    public void Dispose()
    {
        if (_connection == null)
            return;

        _connection.Notification -= OnNotification;
        _connection.Dispose();
    }
}
