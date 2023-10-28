using Content.Shared.Movement.Systems;
using Content.Shared.Verbs;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.Client.Replay.Spectator;

/// <summary>
/// This system handles spawning replay observer ghosts and maintaining their positions when traveling through time.
/// It also blocks most normal interactions, just in case.
/// </summary>
/// <remarks>
/// E.g., if an observer is on a grid, and then jumps forward or backward in time to a point where the grid does not
/// exist, where should the observer go? This attempts to maintain their position and eye rotation or just re-spawns
/// them as needed.
/// </remarks>
public sealed partial class ReplaySpectatorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly IReplayPlaybackManager _replayPlayback = default!;

    private SpectatorData? _spectatorData;
    public const string SpectateCmd = "replay_spectate";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<ReplaySpectatorComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ReplaySpectatorComponent, LocalPlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<ReplaySpectatorComponent, EntParentChangedMessage>(OnParentChanged);

        InitializeBlockers();

        _replayPlayback.BeforeSetTick += OnBeforeSetTick;
        _replayPlayback.AfterSetTick += OnAfterSetTick;
        _replayPlayback.ReplayPlaybackStarted += OnPlaybackStarted;
        _replayPlayback.ReplayPlaybackStopped += OnPlaybackStopped;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _replayPlayback.BeforeSetTick -= OnBeforeSetTick;
        _replayPlayback.AfterSetTick -= OnAfterSetTick;
        _replayPlayback.ReplayPlaybackStarted -= OnPlaybackStarted;
        _replayPlayback.ReplayPlaybackStopped -= OnPlaybackStopped;
    }

    private void OnPlaybackStarted(MappingDataNode yamlMappingNode, List<object> objects)
    {
        InitializeMovement();
        _conHost.RegisterCommand(SpectateCmd,
            Loc.GetString("cmd-replay-spectate-desc"),
            Loc.GetString("cmd-replay-spectate-help"),
            SpectateCommand,
            SpectateCompletions);

        if (_replayPlayback.TryGetRecorderEntity(out var recorder))
            SpectateEntity(recorder.Value);
        else
            SetSpectatorPosition(default);
    }

    private void OnPlaybackStopped()
    {
        ShutdownMovement();
        _conHost.UnregisterCommand(SpectateCmd);
    }
}
