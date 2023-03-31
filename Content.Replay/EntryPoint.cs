using Content.Replay.Manager;
using Content.Replay.UI.Menu;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Replay;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly IClientConGroupController _conGrp = default!;

    public override void PreInit()
    {
        base.PreInit();
    }

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
        IoCManager.Resolve<ReplayManager>().Initialize();

        // Despite this being post init, did you know that UI controllers do post-post-init setup?
        // And that means theres no way to just set the current state without a post-post-post init code? fuck this.
        Timer.Spawn(1, () => _stateMan.RequestStateChange<ReplayMainScreen>());
    }
}
