using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.Events;

namespace Content.Server.Nodes.EntitySystems.Autolinkers;

public sealed partial class AnchorStateSensitiveNodeSystem : EntitySystem
{
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorStateSensitiveNodeComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchorStateSensitiveNodeComponent, ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<AnchorStateSensitiveNodeComponent, PolyNodeRelayEvent<AnchorStateChangedEvent>>(OnRelayAnchorStateChanged);
        SubscribeLocalEvent<AnchorStateSensitiveNodeComponent, PolyNodeRelayEvent<ReAnchorEvent>>(OnRelayReAnchor);
    }


    private void OnAnchorStateChanged(EntityUid uid, AnchorStateSensitiveNodeComponent comp, ref AnchorStateChangedEvent args)
    {
        _nodeSystem.QueueEdgeUpdate(uid);
    }

    private void OnReAnchor(EntityUid uid, AnchorStateSensitiveNodeComponent comp, ref ReAnchorEvent args)
    {
        _nodeSystem.QueueEdgeUpdate(uid);
    }

    private void OnRelayAnchorStateChanged(EntityUid uid, AnchorStateSensitiveNodeComponent comp, ref PolyNodeRelayEvent<AnchorStateChangedEvent> args)
    {
        OnAnchorStateChanged(uid, comp, ref args.Event);
    }

    private void OnRelayReAnchor(EntityUid uid, AnchorStateSensitiveNodeComponent comp, ref PolyNodeRelayEvent<ReAnchorEvent> args)
    {
        OnReAnchor(uid, comp, ref args.Event);
    }
}
