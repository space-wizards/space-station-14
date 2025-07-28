using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.Helpers;
using Content.Shared._NullLink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Starlight.NullLink;
using Starlight.NullLink.Abstract;
using static Content.Shared._NullLink.NullLink;
using NLServer = Starlight.NullLink.Server;
using NLServerInfo = Starlight.NullLink.ServerInfo;

namespace Content.Server._NullLink;

public sealed partial class HubSystem : EntitySystem, IServerObserver, IServerInfoObserver
{
    private const int MaxEventsPerTick = 2;

    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IActorRouter _actors = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [Dependency] private readonly IGameTiming _timing = default!;

    private ISawmill _sawmill = default!;

    private static readonly TimeSpan _grainDelay = TimeSpan.FromSeconds(180);
    private static readonly TimeSpan _subLifetime = TimeSpan.FromSeconds(16);
    private TimeSpan? _lastResubscribe;

    private readonly Dictionary<ICommonSession, TimeSpan> _subscriptions = [];
    private readonly List<ICommonSession> _toRemove = [];
    private bool _processingResubscribe = false;

    private ConcurrentDictionary<string, NullLink.Server> _serverData = [];
    private ConcurrentDictionary<string, NullLink.ServerInfo> _serverInfoData = [];
    private readonly ConcurrentQueue<NullLink.UpdateEvent> _updateEvents = [];

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("Hub");
        base.Initialize();
        InitializeServer();
        InitializeServerInfo();

        SubscribeNetworkEvent<NullLink.Subscribe>(OnSubscribe);
        SubscribeNetworkEvent<NullLink.Resubscribe>(OnResubscribe);
        SubscribeNetworkEvent<NullLink.Unsubscribe>(OnUnsubscribe);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_processingResubscribe)
        {
            if ( _actors.Enabled
                && (_lastResubscribe == null || _lastResubscribe + _grainDelay < _timing.RealTime))
            {

                if (_lastResubscribe == null)
                {
                    _processingResubscribe = true;
                    Pipe.RunInBackground(async () =>
                    {
                        await _actors.Connection;
                        if (!_actors.TryGetGrain<IHubGrain>(0, out var hub)
                          || !_actors.TryCreateObjectReference<IServerObserver>(this, out var serverObjectReference)
                          || !_actors.TryCreateObjectReference<IServerInfoObserver>(this, out var serverInfoObjectReference))
                        {
                            _processingResubscribe = false;
                            return;
                        }

                        var serverData = await hub!.GetAndSubscribe(serverObjectReference!);
                        _serverData = new(serverData.Select(x => KeyValuePair.Create(x.Key, Map(x.Value))));

                        var serverInfoData = await hub!.GetAndSubscribe(serverInfoObjectReference!);
                        _serverInfoData = new(serverInfoData.Select(x => KeyValuePair.Create(x.Key, Map(x.Value))));

                        _processingResubscribe = false;
                        _lastResubscribe = _timing.RealTime;

                    }, ex =>
                    {
                        _processingResubscribe = false;
                        _sawmill.Log(LogLevel.Warning, ex, "Failed to resubscribe to server data.");
                        _lastResubscribe = null;
                    });
                }
                else
                {
                    Pipe.RunInBackground(async () =>
                    {
                        await _actors.Connection;
                        if (!_actors.TryGetGrain<IHubGrain>(0, out var hub)
                          || !_actors.TryCreateObjectReference<IServerObserver>(this, out var serverObjectReference)
                          || !_actors.TryCreateObjectReference<IServerInfoObserver>(this, out var serverInfoObjectReference))
                        {
                            _processingResubscribe = false;
                            return;
                        }

                        await hub!.Resubscribe(serverObjectReference!);
                        await hub!.Resubscribe(serverInfoObjectReference!);

                        _processingResubscribe = false;
                        _lastResubscribe = _timing.RealTime;

                    }, ex =>
                    {
                        _processingResubscribe = false;
                        _sawmill.Log(LogLevel.Warning, ex, "Failed to resubscribe to serverinfo data.");
                        _lastResubscribe = null;
                    });
                }
            }
        }

        var processed = 0;
        var now = _timing.RealTime;

        while (processed < MaxEventsPerTick && _updateEvents.TryDequeue(out var ev))
        {
            foreach (var (session, lastSeen) in _subscriptions)
                if (now - lastSeen > _subLifetime)
                    _toRemove.Add(session);
                else
                    RaiseNetworkEvent(ev, session);

            processed++;
        }

        foreach (var dead in _toRemove)
            _subscriptions.Remove(dead);

        _toRemove.Clear();
    }

    private void OnSubscribe(NullLink.Subscribe _, EntitySessionEventArgs args)
    {
        _subscriptions[args.SenderSession] = _timing.RealTime;
        RaiseNetworkEvent(new NullLink.ServerData
        {
            Servers = _serverData.ToDictionary(x => x.Key, x => x.Value),
            ServerInfo = _serverInfoData.ToDictionary(x => x.Key, x => x.Value)
        }, args.SenderSession);
    }

    private void OnResubscribe(NullLink.Resubscribe _, EntitySessionEventArgs args)
        => _subscriptions[args.SenderSession] = _timing.RealTime;
    private void OnUnsubscribe(NullLink.Unsubscribe _, EntitySessionEventArgs args)
        => _subscriptions.Remove(args.SenderSession);

    public Task Updated(string key, NLServer value)
    {
        _serverData.AddOrUpdate(key, Map(value), (k, v) => Map(value));
        _updateEvents.Enqueue(new NullLink.AddOrUpdateServer
        {
            Key = key,
            Server = Map(value)
        });
        return Task.CompletedTask;
    }

    public Task Remove(string key)
    {
        _serverData.TryRemove(key, out _);
        _serverInfoData.TryRemove(key, out _);
        _updateEvents.Enqueue(new NullLink.RemoveServer
        {
            Key = key
        });
        return Task.CompletedTask;
    }

    public Task Updated(string key, NLServerInfo value)
    {
        _serverInfoData.AddOrUpdate(key, Map(value), (k, v) => Map(value));
        _updateEvents.Enqueue(new NullLink.AddOrUpdateServerInfo
        {
            Key = key,
            ServerInfo = Map(value)
        });
        return Task.CompletedTask;
    }

    private static NullLink.Server Map(NLServer server)
        => new()
        {
            Title = server.Title,
            Description = server.Description,
            Type = (NullLink.ServerType)server.Type,
            IsAdultOnly = server.IsAdultOnly,
            ConnectionString = server.ConnectionString
        };
    private static NullLink.ServerInfo Map(NLServerInfo info)
        => new()
        {
            Status = (NullLink.ServerStatus)info.Status,
            Players = info.Players,
            MaxPlayers = info.MaxPlayers,
            СurrentStateStartedAt = info.СurrentStateStartedAt
        };
}