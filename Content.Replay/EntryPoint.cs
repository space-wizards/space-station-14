using Content.Replay.Manager;
using Content.Replay.UI.Menu;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Shared.ContentPack;

namespace Content.Replay;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly ReplayManager _replayMan = default!;
    [Dependency] private readonly IClientConGroupController _conGrp = default!;

    public override void Init()
    {
        base.Init();
        IoCManager.Register<ReplayManager, ReplayManager>();
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();
        _client.StartSinglePlayer();
        _conGrp.Implementation = new ConGroup();
        _replayMan.Initialize();
        _stateMan.RequestStateChange<ReplayMainScreen>();
    }
}
