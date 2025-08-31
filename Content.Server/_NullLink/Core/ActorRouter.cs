using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Content.Server._NullLink.Helpers;
using Content.Shared.CCVar;
using Content.Shared.NullLink.CCVar;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Robust.Shared.Configuration;
using Starlight.NullLink;

namespace Content.Server._NullLink.Core;

public sealed partial class ActorRouter : IActorRouter, IDisposable
{
    [Dependency] private readonly Robust.Shared.Configuration.IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    private string _clusterConnectionString = string.Empty;

    private string? _project;
    private string? _server;

    public bool Enabled { get; private set; }
    public Task Connection => OrleansClientHolder.Connection;
    public event Action OnConnected = () => { };
    private Action? _onConnectedProxy;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("actor-router");

        _onConnectedProxy = () =>
        {
            _sawmill.Info("Attempting to invoke Orleans cluster connection callbacks...");
            try
            {
                OnConnected.Invoke();
                _sawmill.Info("Successfully executed Orleans cluster connection callbacks");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error invoking OnConnected callback, {ex}");
            }
            _sawmill.Info("Connected to Orleans cluster.");
        };
        OrleansClientHolder.OnConnected += _onConnectedProxy;

        _cfg.OnValueChanged(NullLinkCCVars.ClusterConnectionString, OnConnStringChanged, true);
        _cfg.OnValueChanged(NullLinkCCVars.Enabled, OnEnabledChanged, true);

        _cfg.OnValueChanged(NullLinkCCVars.Project, x => _project = x, true);
        _cfg.OnValueChanged(NullLinkCCVars.Server, x => _server = x, true);
    }
    public ValueTask Shutdown()
    {
        OrleansClientHolder.OnConnected -= _onConnectedProxy;
        return OrleansClientHolder.Shutdown();
    }

    public bool TryGetServerGrain([NotNullWhen(true)] out IServerGrain? serverGrain)
    {
        if (!string.IsNullOrEmpty(_project)
            && !string.IsNullOrEmpty(_server)
            && TryGetGrain($"{_project}.{_server}", out serverGrain))
            return true;

        serverGrain = default;
        return false;
    }

    private void OnEnabledChanged(bool enabled)
    {
        Enabled = enabled;
        if (Enabled)
            OrleansClientHolder.Configure(_clusterConnectionString, _sawmill).FireAndForget();
        else
            OrleansClientHolder.Shutdown().FireAndForget();
    }

    private void OnConnStringChanged(string conn)
    {
        _clusterConnectionString = conn;
        if (Enabled)
            OrleansClientHolder.Configure(_clusterConnectionString, _sawmill, rebuild: true).FireAndForget();
    }

    public void Dispose()
    {
        OrleansClientHolder.Shutdown();
    }
}
