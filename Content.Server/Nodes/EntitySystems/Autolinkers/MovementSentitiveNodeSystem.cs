using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.Events;

namespace Content.Server.Nodes.EntitySystems.Autolinkers;

public sealed partial class MovementSensitiveNodeSystem : EntitySystem
{
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementSensitiveNodeComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<MovementSensitiveNodeComponent, PolyNodeRelayEvent<MoveEvent>>(OnRelayMove);
    }


    private void OnMove(EntityUid uid, MovementSensitiveNodeComponent comp, ref MoveEvent args)
    {
        if (args.NewPosition != args.OldPosition || comp.DirtyOnRotation && args.NewRotation != args.OldRotation)
            _nodeSystem.QueueEdgeUpdate(uid);
    }

    private void OnRelayMove(EntityUid uid, MovementSensitiveNodeComponent comp, ref PolyNodeRelayEvent<MoveEvent> args)
    {
        OnMove(uid, comp, ref args.Event);
    }
}
