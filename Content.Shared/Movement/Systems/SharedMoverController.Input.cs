using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /*
     * This is a big and scary class.
     * There is going to be boilerplate around that I couldn't think of a decent way to remove.
     * Handles all movement related inputs and subtick support for ALL mover classes.
     * The actual mover code for each mob is under its respective partial class.
     */

    public bool DiagonalMovementEnabled = true;

    private void InitializeInput()
    {
        /*
         * Okay here's the plan.
         * Vehicles use mobmover probably
         * Shuttles use their own movers.
         * Jetpacks use mobmover
         * KinematicMover is its own thing with no walk key.
         *
         * Just copying mobmover to shuttles shouldn't be THAT much boilerplate I don't think, especially with the
         * subtick handling boilerplate removed.
         *
         * Add a helper method for VelocityDir that considers remaining fraction and adds it onto the thing.
         */

        var moveUpCmdHandler = new MoverDirInputCmdHandler(EntityManager, this, MoveButtons.Up);
        var moveLeftCmdHandler = new MoverDirInputCmdHandler(EntityManager, this, MoveButtons.Left);
        var moveRightCmdHandler = new MoverDirInputCmdHandler(EntityManager, this, MoveButtons.Right);
        var moveDownCmdHandler = new MoverDirInputCmdHandler(EntityManager, this, MoveButtons.Down);

        CommandBinds.Builder
            // Mob + Jetpack movement
            .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
            .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
            .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
            .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
            .Bind(EngineKeyFunctions.Walk, new WalkInputCmdHandler(EntityManager, this))
            // Rider
            // TODO: Relay to vehicle if we don't have vehiclecomponent.
            // Shuttle
            .Bind(ContentKeyFunctions.ShuttleBrake, new ShuttleBrakeInputCmdHandler())

            .Register<Systems.SharedMoverController>();

        // TODO: Space brakes
    }

    private void ShutdownInput()
    {
        CommandBinds.Unregister<Systems.SharedMoverController>();
    }

    #region MobMover

    private void SetMoveInput(MobMoverComponent component, ushort subTick, bool enabled, MoveButtons bit)
    {
        TryResetSubtickInput(component, out _);

        if (TryGetSubtick(component, subTick, out var fraction))
        {
            ref var lastMoveAmount = ref component.Sprinting ? ref component.CurTickSprintMovement : ref component.CurTickWalkMovement;
            lastMoveAmount += DirVecForButtons(component.HeldMoveButtons) * fraction;
        }

        if (enabled)
        {
            component.HeldMoveButtons |= bit;
        }
        else
        {
            component.HeldMoveButtons &= ~bit;
        }

        // TODO: Is this even needed?
        Dirty(component);
    }

    private void SetSprintInput(MobMoverComponent component, ushort subTick, bool enabled)
    {
        TryResetSubtickInput(component, out _);

        if (TryGetSubtick(component, subTick, out var fraction))
        {
            ref var lastMoveAmount = ref component.Sprinting ? ref component.CurTickSprintMovement : ref component.CurTickWalkMovement;
            lastMoveAmount += DirVecForButtons(component.HeldMoveButtons) * fraction;
        }

        component.Sprinting = !enabled;

        // TODO: Is this even needed?
        Dirty(component);
    }

    /// <summary>
    /// Gets the movement input for this mob mover at this point in time.
    /// </summary>
    public (Vector2 Walk, Vector2 Sprint) GetMobVelocityInput(MobMoverComponent component)
    {
        if (!_gameTiming.InSimulation)
        {
            // Outside of simulation we'll be running client predicted movement per-frame.
            // So return a full-length vector as if it's a full tick.
            // Physics system will have the correct time step anyways.
            var immediateDir = DirVecForButtons(component.HeldMoveButtons);
            return component.Sprinting ? (Vector2.Zero, immediateDir) : (immediateDir, Vector2.Zero);
        }

        Vector2 walk;
        Vector2 sprint;

        // We need to work out, since the last time we did our subtick input, how much fraction is remaining and add that on
        if (!TryResetSubtickInput(component, out var remainingFraction))
        {
            walk = Vector2.Zero;
            sprint = Vector2.Zero;
        }
        else
        {
            walk = component.CurTickWalkMovement;
            sprint = component.CurTickSprintMovement;
        }

        var curDir = DirVecForButtons(component.HeldMoveButtons) * remainingFraction;

        if (component.Sprinting)
        {
            sprint += curDir;
        }
        else
        {
            walk += curDir;
        }

        return (walk, sprint);
    }

    private sealed class MoverDirInputCmdHandler : InputCmdHandler
    {
        private IEntityManager _entManager;
        private Systems.SharedMoverController _controller;
        private readonly MoveButtons _dir;

        public MoverDirInputCmdHandler(IEntityManager entManager, Systems.SharedMoverController controller, MoveButtons dir)
        {
            _entManager = entManager;
            _controller = controller;
            _dir = dir;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full ||
                !_entManager.TryGetComponent<MobMoverComponent>(session?.AttachedEntity, out var mover))
                return false;

            _controller.SetMoveInput(mover, message.SubTick, full.State == BoundKeyState.Down, _dir);
            return false;
        }
    }

    private sealed class WalkInputCmdHandler : InputCmdHandler
    {
        private IEntityManager _entManager;
        private Systems.SharedMoverController _controller;

        public WalkInputCmdHandler(IEntityManager entManager, Systems.SharedMoverController controller)
        {
            _entManager = entManager;
            _controller = controller;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full ||
                !_entManager.TryGetComponent<MobMoverComponent>(session?.AttachedEntity, out var mobMover))
            {
                return false;
            }

            _controller.SetSprintInput(mobMover, message.SubTick, full.State != BoundKeyState.Down);
            return false;
        }
    }

    #endregion

    #region Rider



    #endregion

    #region Shuttles

    private sealed class ShuttleBrakeInputCmdHandler : InputCmdHandler
    {
        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full) return false;

            // TODO: Set shuttle stuffsies.

            return false;
        }
    }

    #endregion

    /// <summary>
    ///     Retrieves the normalized direction vector for a specified combination of movement keys.
    /// </summary>
    private Vector2 DirVecForButtons(MoveButtons buttons)
    {
        // key directions are in screen coordinates
        // _moveDir is in world coordinates
        // if the camera is moved, this needs to be changed

        var x = 0;
        x -= HasFlag(buttons, MoveButtons.Left) ? 1 : 0;
        x += HasFlag(buttons, MoveButtons.Right) ? 1 : 0;

        var y = 0;

        if (DiagonalMovementEnabled || x == 0)
        {
            y -= HasFlag(buttons, MoveButtons.Down) ? 1 : 0;
            y += HasFlag(buttons, MoveButtons.Up) ? 1 : 0;
        }

        var vec = new Vector2(x, y);

        // can't normalize zero length vector
        if (vec.LengthSquared > 1.0e-6)
        {
            // Normalize so that diagonals aren't faster or something.
            vec = vec.Normalized;
        }

        return vec;
    }


    private static bool HasFlag(MoveButtons buttons, MoveButtons flag)
    {
        return (buttons & flag) == flag;
    }

    /// <summary>
    /// If the last tick we handled input on is older than the current tick then reset all of our values.
    /// </summary>
    private bool TryResetSubtickInput(MoverComponent component, out float remainingFraction)
    {
        // Reset the input if its last input was on a previous tick
        if (_gameTiming.CurTick <= component.LastInputTick)
        {
            remainingFraction = 1f;
            return false;
        }

        component.LastInputTick = _gameTiming.CurTick;
        component.LastInputSubTick = 0;
        remainingFraction = (ushort.MaxValue - component.LastInputSubTick) / (float) ushort.MaxValue;
        return true;
    }

    /// <summary>
    /// Try to get the fraction that this subtick is into the tick and update.
    /// </summary>
    /// <returns></returns>
    private bool TryGetSubtick(MoverComponent component, ushort subTick, out float fraction)
    {
        fraction = 0f;

        if (subTick < component.LastInputSubTick) return false;

        fraction = (subTick - component.LastInputSubTick) / (float) ushort.MaxValue;
        component.LastInputSubTick = subTick;
        return true;
    }
}

[Flags]
public enum MoveButtons : byte
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
}
