using Content.Shared.Maps;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Maps;

/// <inheritdoc />
public sealed class GridDraggingSystem : SharedGridDraggingSystem
{
    [Dependency] private readonly IConGroupController _admin = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<ICommonSession> _draggers = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GridDragRequestPosition>(OnRequestDrag);
        SubscribeNetworkEvent<GridDragVelocityRequest>(OnRequestVelocity);
    }

    public bool IsEnabled(ICommonSession session) => _draggers.Contains(session);

    public void Toggle(ICommonSession session)
    {
        if (session is not IPlayerSession pSession)
            return;

        DebugTools.Assert(_admin.CanCommand(pSession, CommandName));

        // Weird but it's a toggle
        if (_draggers.Add(session))
        {

        }
        else
        {
            _draggers.Remove(session);
        }

        RaiseNetworkEvent(new GridDragToggleMessage()
        {
            Enabled = _draggers.Contains(session),
        }, session.ConnectedClient);
    }

    private void OnRequestVelocity(GridDragVelocityRequest ev, EntitySessionEventArgs args)
    {
        var grid = GetEntity(ev.Grid);

        if (args.SenderSession is not IPlayerSession playerSession ||
            !_admin.CanCommand(playerSession, CommandName) ||
            !Exists(grid) ||
            Deleted(grid))
        {
            return;
        }

        var gridBody = Comp<PhysicsComponent>(grid);
        _physics.SetLinearVelocity(grid, ev.LinearVelocity, body: gridBody);
        _physics.SetAngularVelocity(grid, 0f, body: gridBody);
    }

    private void OnRequestDrag(GridDragRequestPosition msg, EntitySessionEventArgs args)
    {
        var grid = GetEntity(msg.Grid);

        if (args.SenderSession is not IPlayerSession playerSession ||
            !_admin.CanCommand(playerSession, CommandName) ||
            !Exists(grid) ||
            Deleted(grid))
        {
            return;
        }

        var gridXform = Transform(grid);

        gridXform.WorldPosition = msg.WorldPosition;
    }
}
