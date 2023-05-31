using Robust.Client;
using Robust.Client.Audio.Midi;
using Robust.Client.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Replays.Loading;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.Upload;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Replay.Manager;

public sealed partial class ReplayManager
{
    [Dependency] private readonly IMidiManager _midi = default!;
    [Dependency] private readonly IClydeAudio _clydeAudio = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IClientNetManager _netMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameController _controller = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IClientEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IConfigurationManager _confMan = default!;
    [Dependency] private readonly NetworkResourceManager _netResMan = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientNetConfigurationManager _netConf = default!;

    public ReplayData? CurrentReplay;

    private int _checkpointInterval;
    private int _visualEventThreshold;
    public int ScrubbingIndex;
    public bool ActivelyScrubbing = false;
    public int? Steps;
    private bool _playing;

    public CheckpointState GetLastCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, (~checkpointIndex) - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index <= index);
        DebugTools.Assert(checkpointIndex == data.Checkpoints.Length - 1 || data.Checkpoints[checkpointIndex + 1].Index > index);
        return checkpoint;
    }

    public CheckpointState GetNextCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, (~checkpointIndex) - 1);

        checkpointIndex = Math.Clamp(checkpointIndex + 1, 0, data.Checkpoints.Length - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index >= index || checkpointIndex == data.Checkpoints.Length - 1);
        return checkpoint;
    }


    public bool Playing
    {
        get => _playing && !ActivelyScrubbing;
        set
        {
            if (_playing && !value)
            {
                StopAudio();
                Steps = null;
            }

            _playing = value;
        }
    }

    private bool _initialized;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _sawmill = Logger.GetSawmill("replay");
        _confMan.OnValueChanged(CVars.CheckpointInterval, (value) => _checkpointInterval = value, true);

        _confMan.OnValueChanged(CVars.VisualEventThreshold, (value) => _visualEventThreshold = value, true);
    }
}
