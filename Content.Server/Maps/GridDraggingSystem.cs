using Content.Shared.Maps;
using Robust.Server.Console;
using Robust.Server.Player;

namespace Content.Server.Maps;

/// <inheritdoc />
public sealed class GridDraggingSystem : SharedGridDraggingSystem
{
    [Dependency] private readonly IConGroupController _admin = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GridDragRequestPosition>(OnRequestDrag);
        SubscribeNetworkEvent<GridDragVelocityRequest>(OnRequestVelocity);
    }

    private void OnRequestVelocity(GridDragVelocityRequest ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not IPlayerSession playerSession ||
            !_admin.CanCommand(playerSession, CommandName) ||
            !Exists(ev.Grid) ||
            Deleted(ev.Grid)) return;

        var gridBody = Comp<PhysicsComponent>(ev.Grid);
        gridBody.LinearVelocity = ev.LinearVelocity;
        gridBody.AngularVelocity = 0f;
    }

    private void OnRequestDrag(GridDragRequestPosition msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not IPlayerSession playerSession ||
            !_admin.CanCommand(playerSession, CommandName) ||
            !Exists(msg.Grid) ||
            Deleted(msg.Grid)) return;

        var gridXform = Transform(msg.Grid);

        gridXform.WorldPosition = msg.WorldPosition;
    }
}
