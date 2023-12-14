using Content.Shared.Movement.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Replay.Spectator;

// Partial class handles movement logic for observers.
public sealed partial class ReplaySpectatorSystem
{
    public DirectionFlag Direction;

    /// <summary>
    /// Fallback speed if the observer ghost has no <see cref="MovementSpeedModifierComponent"/>.
    /// </summary>
    public const float DefaultSpeed = 12;

    private void InitializeMovement()
    {
        var moveUpCmdHandler = new MoverHandler(this, DirectionFlag.North);
        var moveLeftCmdHandler = new MoverHandler(this, DirectionFlag.West);
        var moveRightCmdHandler = new MoverHandler(this, DirectionFlag.East);
        var moveDownCmdHandler = new MoverHandler(this, DirectionFlag.South);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
            .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
            .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
            .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
            .Register<ReplaySpectatorSystem>();
    }

    private void ShutdownMovement()
    {
        CommandBinds.Unregister<ReplaySpectatorSystem>();
    }

    // Normal mover code works via physics. Replays don't do prediction/physics. You can fudge it by relying on the
    // fact that only local-player physics is currently predicted, but instead I've just added crude mover logic here.
    // This just runs on frame updates, no acceleration or friction here.
    public override void FrameUpdate(float frameTime)
    {
        if (_replayPlayback.Replay == null)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (Direction == DirectionFlag.None)
        {
            if (TryComp(player, out InputMoverComponent? cmp))
                _mover.LerpRotation(player, cmp, frameTime);

            return;
        }

        if (!IsClientSide(player) || !HasComp<ReplaySpectatorComponent>(player))
        {
            // Player is trying to move -> behave like the ghost-on-move component.
            SpawnSpectatorGhost(new EntityCoordinates(player, default), true);
            return;
        }

        if (!TryComp(player, out InputMoverComponent? mover))
            return;

        _mover.LerpRotation(player, mover, frameTime);

        var effectiveDir = Direction;
        if ((Direction & DirectionFlag.North) != 0)
            effectiveDir &= ~DirectionFlag.South;

        if ((Direction & DirectionFlag.East) != 0)
            effectiveDir &= ~DirectionFlag.West;

        var query = GetEntityQuery<TransformComponent>();
        var xform = query.GetComponent(player);
        var pos = _transform.GetWorldPosition(xform);

        if (!xform.ParentUid.IsValid())
        {
            // Were they sitting on a grid as it was getting deleted?
            SetSpectatorPosition(default);
            return;
        }

        // A poor mans grid-traversal system. Should also interrupt ghost-following.
        // This is very hacky and has already caused bugs.
        // This is done the way it is because grid traversal gets processed in physics' SimulateWorld() update.
        // TODO do this properly somehow.
        _transform.SetGridId(player, xform, null);
        _transform.AttachToGridOrMap(player);
        if (xform.ParentUid.IsValid())
            _transform.SetGridId(player, xform, Transform(xform.ParentUid).GridUid);

        var parentRotation = _mover.GetParentGridAngle(mover);
        var localVec = effectiveDir.AsDir().ToAngle().ToWorldVec();
        var worldVec = parentRotation.RotateVec(localVec);
        var speed = CompOrNull<MovementSpeedModifierComponent>(player)?.BaseSprintSpeed ?? DefaultSpeed;
        var delta = worldVec * frameTime * speed;
        _transform.SetWorldPositionRotation(player, pos + delta, delta.ToWorldAngle(), xform);
    }

    private sealed class MoverHandler : InputCmdHandler
    {
        private readonly ReplaySpectatorSystem _sys;
        private readonly DirectionFlag _dir;

        public MoverHandler(ReplaySpectatorSystem sys, DirectionFlag dir)
        {
            _sys = sys;
            _dir = dir;
        }

        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (message.State == BoundKeyState.Down)
                _sys.Direction |= _dir;
            else
                _sys.Direction &= ~_dir;

            return true;
        }
    }
}
