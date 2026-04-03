using Content.Client.Replay;
using Content.Replay.Menu;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Shared.ContentPack;

namespace Content.Replay;

[UsedImplicitly]
public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly ContentReplayPlaybackManager _contentReplayPlaybackMan = default!;
    [Dependency] private readonly IClientConGroupController _conGrp = default!;

    public override void Init()
    {
        base.Init();
        Dependencies.BuildGraph();
        Dependencies.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();
        _client.StartSinglePlayer();
        _conGrp.Implementation = new ReplayConGroup();
        _contentReplayPlaybackMan.DefaultState = typeof(ReplayMainScreen);
        _stateMan.RequestStateChange<ReplayMainScreen>();
    }
}
