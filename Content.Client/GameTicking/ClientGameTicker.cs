using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.GameWindow;
using Content.Shared.Maps;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.GameTicking;

[UsedImplicitly]
public sealed partial class ClientGameTicker : GameTicker
{
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;

    private Dictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>>  _jobsAvailable = new();
    private Dictionary<NetEntity, string> _stationNames = new();
    private Dictionary<NetEntity, ProtoId<JobWeightPrototype>?> _jobWeightsByStation = new();

    [ViewVariables] public bool AreWeReady { get; private set; }
    [ViewVariables] public bool IsGameStarted { get; private set; }
    [ViewVariables] public ResolvedSoundSpecifier? RestartSound { get; private set; }
    [ViewVariables] public ProtoId<LobbyBackgroundPrototype>? LobbyBackground { get; private set; }
    [ViewVariables] public bool DisallowedLateJoin { get; private set; }
    [ViewVariables] public string? ServerInfoBlob { get; private set; }
    [ViewVariables] public TimeSpan StartTime { get; private set; }
    [ViewVariables] public new bool Paused { get; private set; }

    [ViewVariables] public IReadOnlyDictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>> JobsAvailable => _jobsAvailable;
    [ViewVariables] public IReadOnlyDictionary<NetEntity, string> StationNames => _stationNames;
    [ViewVariables] public IReadOnlyDictionary<NetEntity, ProtoId<JobWeightPrototype>?> JobWeightsByStation => _jobWeightsByStation;

    public event Action? InfoBlobUpdated;
    public event Action? LobbyStatusUpdated;
    public event Action? LobbyLateJoinStatusUpdated;
    public event Action<IReadOnlyDictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>>>? LobbyJobsAvailableUpdated;

    public override void Initialize()
    {
        base.Initialize();

        _admin.AdminStatusUpdated += OnAdminUpdated;
        OnAdminUpdated();
    }

    public override void Shutdown()
    {
        _admin.AdminStatusUpdated -= OnAdminUpdated;
        base.Shutdown();
    }

    public override IReadOnlyList<EntityUid> LoadGameMap(GameMapPrototype proto,
        out MapId mapId,
        DeserializationOptions? options = null,
        string? stationName = null,
        Vector2? offset = null,
        Angle? rot = null)
    {
        throw new NotImplementedException();
    }

    public override int ReadyPlayerCount()
    {
        throw new NotImplementedException();
    }

    public override void PlayerJoinGame(ICommonSession session, bool silent = false)
    {
        // TODO: Can probably move some logic into here :P
    }

    private void OnAdminUpdated()
    {
        // TODO: We REALLY should not be logging parent changed events for parents the client cannot see :V
        // Hide some map/grid related logs from clients. This is to try prevent some easy metagaming by just
        // reading the console. E.g., logs like this one could leak the nuke station/grid:
        // > Grid NT-Arrivals 1101 (122/n25896) changed parent. Old parent: map 10 (121/n25895). New parent: FTL (123/n26470)
#if !DEBUG
            EntityManager.System<SharedMapSystem>().Log.Level = _admin.IsAdmin() ? LogLevel.Info : LogLevel.Warning;
#endif
    }

    [SubscribeNetworkEvent]
    private void OnAttentionRequest(RequestWindowAttentionEvent ev)
    {
        _clyde.RequestWindowAttention();
    }

    [SubscribeNetworkEvent]
    private void LateJoinStatus(TickerLateJoinStatusEvent message)
    {
        DisallowedLateJoin = message.Disallowed;
        LobbyLateJoinStatusUpdated?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void UpdateJobsAvailable(TickerJobsAvailableEvent message)
    {
        _jobsAvailable.Clear();

        foreach (var (job, data) in message.JobsAvailableByStation)
        {
            _jobsAvailable[job] = data;
        }

        _stationNames.Clear();
        foreach (var weh in message.StationNames)
        {
            _stationNames[weh.Key] = weh.Value;
        }

        _jobWeightsByStation.Clear();
        foreach (var (station, jobWeights) in message.JobWeightsByStation)
        {
            _jobWeightsByStation[station] = jobWeights;
        }

        LobbyJobsAvailableUpdated?.Invoke(JobsAvailable);
    }

    [SubscribeNetworkEvent]
    private void JoinLobby(TickerJoinLobbyEvent message)
    {
        _stateManager.RequestStateChange<LobbyState>();
    }

    [SubscribeNetworkEvent]
    private void ConnectionStatus(TickerConnectionStatusEvent message)
    {
        RoundStartTimeSpan = message.RoundStartTimeSpan;
    }

    [SubscribeNetworkEvent]
    private void LobbyStatus(TickerLobbyStatusEvent message)
    {
        StartTime = message.StartTime;
        RoundStartTimeSpan = message.RoundStartTimeSpan;
        IsGameStarted = message.IsRoundStarted;
        AreWeReady = message.YouAreReady;
        LobbyBackground = message.LobbyBackground;
        Paused = message.Paused;

        LobbyStatusUpdated?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void LobbyInfo(TickerLobbyInfoEvent message)
    {
        ServerInfoBlob = message.TextBlob;

        InfoBlobUpdated?.Invoke();
    }

    [SubscribeNetworkEvent]
    private void JoinGame(TickerJoinGameEvent message)
    {
        _stateManager.RequestStateChange<GameplayState>();
    }

    [SubscribeNetworkEvent]
    private void LobbyCountdown(TickerLobbyCountdownEvent message)
    {
        StartTime = message.StartTime;
        Paused = message.Paused;
    }

    [SubscribeNetworkEvent]
    private void RoundEnd(RoundEndMessageEvent message)
    {
        // Force an update in the event of this song being the same as the last.
        RestartSound = message.RestartSound;

        _userInterfaceManager.GetUIController<RoundEndSummaryUIController>().OpenRoundEndSummaryWindow(message);
    }
}
