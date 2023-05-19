using Content.Client.Administration.Managers;
using Robust.Client;
using Robust.Client.Audio.Midi;
using Robust.Client.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Serialization;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Replay.Manager;

public sealed partial class ReplayManager
{
    [Dependency] private readonly IMidiManager _midi = default!;
    [Dependency] private readonly IClydeAudio _clydeAudio = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IClientNetManager _netMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IGameController _controller = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ILocalizationManager _locMan = default!;
    [Dependency] private readonly IClientEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IConfigurationManager _confMan = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;
    [Dependency] private readonly NetworkResourceManager _netResMan = default!;
    [Dependency] private readonly IClientNetConfigurationManager _netConf = default!;

    public ReplayData? CurrentReplay;

    private int _visualEventThreshold;
    private int _checkpointInterval;
    private int _checkpointEntitySpawnThreshold;
    private int _checkpointEntityStateThreshold;

    public int ScrubbingIndex;
    public bool ActivelyScrubbing = false;
    public int? Steps;
    private bool _playing;

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
    private ushort _metaCompNetId;

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _confMan.OnValueChanged(ReplayCvars.VisualEventThreshold, (value) => _visualEventThreshold = value, true);
        _confMan.OnValueChanged(ReplayCvars.CheckpointInterval, (value) => _checkpointInterval = value, true);
        _confMan.OnValueChanged(ReplayCvars.CheckpointEntitySpawnThreshold, (value) => _checkpointEntitySpawnThreshold = value, true);
        _confMan.OnValueChanged(ReplayCvars.CheckpointEntityStateThreshold, (value) => _checkpointEntityStateThreshold = value, true);
        _metaId = _factory.GetRegistration(typeof(MetaDataComponent)).NetID!.Value;
        _sawmill = Logger.GetSawmill("replay");

        var metaId = _factory.GetRegistration(typeof(MetaDataComponent)).NetID;
        _metaCompNetId = metaId!.Value;
    }
}
