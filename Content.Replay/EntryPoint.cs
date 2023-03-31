using Content.Replay.Manager;
using Content.Replay.UI.Menu;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Replay;

// TODO REPLAYS
// - observer movement.
// -- Add "ghost on move" like behavior.
// - Split UI layout.
// - ReplayGhostState code de-duplication
// - command to jump to a specific tick (not index)
// - command variants for time instead of index
// - command localizations
// - Functional StopAudio() / midi handling.
// - teleport UI (players, grids, etc).
// - dynamic checkpoints? See comments in GenerateCheckpoints()
// - reverse states?  See comments in GenerateCheckpoints()
// - hold down fast forward button.
// - properly handle RoundEndMessageEvent
// - improve visual event handling?
// - predicted examine.
// - verbs
// - Figure out a better way of handling screen states (see comments below).

// Currently in order to properly mimic what a given player sees in game, we simply re-use the normal game play screen
// state while directly observing from a player's POV. But this means you can't access the time widget... so ... uhhh. I
// dunno.

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
