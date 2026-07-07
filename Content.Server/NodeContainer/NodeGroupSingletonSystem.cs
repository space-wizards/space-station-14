using Content.Server.GameTicking.Events;
using Content.Shared.NodeContainer.Components;
using Robust.Server.GameStates;

namespace Content.Server.NodeContainer;

public sealed partial class NodeGroupSingletonSystem : EntitySystem
{


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

    }

    private void OnRoundStart(RoundStartingEvent ev)
    {

    }


}
