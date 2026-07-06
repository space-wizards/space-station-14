using Content.Server.GameTicking.Events;
using Content.Shared.NodeContainer.Components;
using Robust.Server.GameStates;

namespace Content.Server.NodeContainer;

public sealed partial class NodeGroupSingletonSystem : EntitySystem
{
    [Dependency] private PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<NodeGroupManagerComponent, MapInitEvent>(OnManagerInit);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var ent = Spawn();
        EnsureComp<NodeGroupManagerComponent>(ent);
    }

    private void OnManagerInit(Entity<NodeGroupManagerComponent> ent, ref MapInitEvent args)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
