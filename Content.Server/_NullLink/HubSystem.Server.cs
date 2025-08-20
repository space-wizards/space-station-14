using System.Threading.Tasks;
using Content.Server._NullLink.Helpers;
using Content.Shared.CCVar;
using Content.Shared.NullLink.CCVar;
using ServerType = Starlight.NullLink.ServerType;

namespace Content.Server._NullLink;

public sealed partial class HubSystem : EntitySystem
{
    private string? _hostname;
    private string? _connectionString;
    private string? _description;
    private bool? _isAdultOnly;
    private ServerType _serverType = ServerType.NRP;

    public void InitializeServer()
    {
        _sawmill = _logManager.GetSawmill("Hub");

        _cfg.OnValueChanged(NullLinkCCVars.Description, OnGameDescChanged, true);
        _cfg.OnValueChanged(NullLinkCCVars.Type, OnServerTypeChanged, true);
        _cfg.OnValueChanged(NullLinkCCVars.Title, OnGameHostNameChanged, true);
        _cfg.OnValueChanged(NullLinkCCVars.IsAdultOnly, OnIsAdultOnlyChanged, true);
        _cfg.OnValueChanged(CCVars.HubServerUrl, OnConnectionStringChanged, true);
    }

    private void OnIsAdultOnlyChanged(bool isAdultOnly)
    {
        _isAdultOnly = isAdultOnly;
        TryUpdateServer();
    }

    private void OnServerTypeChanged(Shared.NullLink.CCVar.ServerType type)
    {
        _serverType = (ServerType)type;
        TryUpdateServer();
    }

    private void OnGameHostNameChanged(string hostname)
    {
        _hostname = hostname;
        TryUpdateServer();
    }

    private void OnConnectionStringChanged(string connectionString)
    {
        _connectionString = connectionString;
        TryUpdateServer();
    }
    private void OnGameDescChanged(string gameDesc)
    {
        _description = gameDesc;
        TryUpdateServer();
    }

    private void TryUpdateServer()
    {
        if (string.IsNullOrEmpty(_hostname)
            || string.IsNullOrEmpty(_connectionString)
            || !_actors.Enabled
            || _isAdultOnly is null)
            return;

        Pipe.RunInBackgroundVT(async () =>
        {
            const int MaxRetries = 3;
            const int BackoffMs = 5000;

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (!_actors.TryGetServerGrain(out var serverGrain))
                    {
                        await Task.Delay(BackoffMs);
                        continue;
                    }

                    await serverGrain.UpdateServer(new()
                    {
                        Title = _hostname,
                        ConnectionString = _connectionString,
                        Description = _description,
                        IsAdultOnly = _isAdultOnly ?? false,
                        Type = _serverType
                    });
                    return;
                }
                catch when (attempt < MaxRetries)
                {
                    await Task.Delay(BackoffMs);
                }
            }
        }, ex => _sawmill.Log(LogLevel.Warning, ex, "NullLink – failed to update server in the cluster."));
    }
}