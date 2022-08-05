using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems
{
    /// <summary>
    ///     Handles converting inputs into movement.
    /// </summary>
    public abstract partial class SharedMoverController
    {
        private void InitializeInput()
        {
            var moveUpCmdHandler = new MoverDirInputCmdHandler(this, Direction.North);
            var moveLeftCmdHandler = new MoverDirInputCmdHandler(this, Direction.West);
            var moveRightCmdHandler = new MoverDirInputCmdHandler(this, Direction.East);
            var moveDownCmdHandler = new MoverDirInputCmdHandler(this, Direction.South);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
                .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
                .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
                .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
                .Bind(EngineKeyFunctions.Walk, new WalkInputCmdHandler(this))
                // TODO: Relay
                // Shuttle
                .Bind(ContentKeyFunctions.ShuttleStrafeUp, new ShuttleInputCmdHandler(this, ShuttleButtons.StrafeUp))
                .Bind(ContentKeyFunctions.ShuttleStrafeLeft, new ShuttleInputCmdHandler(this, ShuttleButtons.StrafeLeft))
                .Bind(ContentKeyFunctions.ShuttleStrafeRight, new ShuttleInputCmdHandler(this, ShuttleButtons.StrafeRight))
                .Bind(ContentKeyFunctions.ShuttleStrafeDown, new ShuttleInputCmdHandler(this, ShuttleButtons.StrafeDown))
                .Bind(ContentKeyFunctions.ShuttleRotateLeft, new ShuttleInputCmdHandler(this, ShuttleButtons.RotateLeft))
                .Bind(ContentKeyFunctions.ShuttleRotateRight, new ShuttleInputCmdHandler(this, ShuttleButtons.RotateRight))
                .Bind(ContentKeyFunctions.ShuttleBrake, new ShuttleInputCmdHandler(this, ShuttleButtons.Brake))
                .Register<SharedMoverController>();

            SubscribeLocalEvent<InputMoverComponent, ComponentInit>(OnInputInit);
            SubscribeLocalEvent<InputMoverComponent, ComponentGetState>(OnInputGetState);
            SubscribeLocalEvent<InputMoverComponent, ComponentHandleState>(OnInputHandleState);
        }

        private void SetMoveInput(InputMoverComponent component, MoveButtons buttons)
        {
            if (component.HeldMoveButtons == buttons) return;
            component.HeldMoveButtons = buttons;
            Dirty(component);
        }

        private void OnInputHandleState(EntityUid uid, InputMoverComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not InputMoverComponentState state) return;
            component.HeldMoveButtons = state.Buttons;
            component.LastInputTick = GameTick.Zero;
            component.LastInputSubTick = 0;
            component.CanMove = state.CanMove;
        }

        private void OnInputGetState(EntityUid uid, InputMoverComponent component, ref ComponentGetState args)
        {
            args.State = new InputMoverComponentState(component.HeldMoveButtons, component.CanMove);
        }

        private void ShutdownInput()
        {
            CommandBinds.Unregister<SharedMoverController>();
        }

        public bool DiagonalMovementEnabled => _configManager.GetCVar(CCVars.GameDiagonalMovement);

        protected virtual void HandleShuttleInput(EntityUid uid, ShuttleButtons button, ushort subTick, bool state) {}

        private void HandleDirChange(EntityUid entity, Direction dir, ushort subTick, bool state)
        {
            // Relayed movement just uses the same keybinds given we're moving the relayed entity
            // the same as us.
            TryComp<InputMoverComponent>(entity, out var moverComp);

            // Can't relay inputs if you're dead.
            if (TryComp<RelayInputMoverComponent>(entity, out var relayMover) && !_mobState.IsIncapacitated(entity))
            {
                // if we swap to relay then stop our existing input if we ever change back.
                if (moverComp != null)
                {
                    SetMoveInput(moverComp, MoveButtons.None);
                }

                if (relayMover.RelayEntity == null) return;

                HandleDirChange(relayMover.RelayEntity.Value, dir, subTick, state);
                return;
            }

            if (moverComp == null)
                return;

            // Relay the fact we had any movement event.
            // TODO: Ideally we'd do these in a tick instead of out of sim.
            var owner = moverComp.Owner;
            var moveEvent = new MoveInputEvent(entity);
            RaiseLocalEvent(owner, ref moveEvent);

            // For stuff like "Moving out of locker" or the likes
            // We'll relay a movement input to the parent.
            if (_container.IsEntityInContainer(owner) &&
                TryComp<TransformComponent>(owner, out var xform) &&
                xform.ParentUid.IsValid() &&
                _mobState.IsAlive(owner))
            {
                var relayMoveEvent = new ContainerRelayMovementEntityEvent(owner);
                RaiseLocalEvent(xform.ParentUid, ref relayMoveEvent);
            }

            SetVelocityDirection(moverComp, dir, subTick, state);
        }

        private void OnInputInit(EntityUid uid, InputMoverComponent component, ComponentInit args)
        {
            var xform = Transform(uid);

            if (!xform.ParentUid.IsValid()) return;

            component.LastGridAngle = Transform(xform.ParentUid).WorldRotation;
        }

        private void HandleRunChange(EntityUid uid, ushort subTick, bool walking)
        {
            TryComp<InputMoverComponent>(uid, out var moverComp);

            if (TryComp<RelayInputMoverComponent>(uid, out var relayMover))
            {
                // if we swap to relay then stop our existing input if we ever change back.
                if (moverComp != null)
                {
                    SetMoveInput(moverComp, MoveButtons.None);
                }

                if (relayMover.RelayEntity == null) return;

                HandleRunChange(relayMover.RelayEntity.Value, subTick, walking);
                return;
            }

            if (moverComp == null) return;

            SetSprinting(moverComp, subTick, walking);
        }

        public (Vector2 Walking, Vector2 Sprinting) GetVelocityInput(InputMoverComponent mover)
        {
            if (!Timing.InSimulation)
            {
                // Outside of simulation we'll be running client predicted movement per-frame.
                // So return a full-length vector as if it's a full tick.
                // Physics system will have the correct time step anyways.
                var immediateDir = DirVecForButtons(mover.HeldMoveButtons);
                return mover.Sprinting ? (Vector2.Zero, immediateDir) : (immediateDir, Vector2.Zero);
            }

            Vector2 walk;
            Vector2 sprint;
            float remainingFraction;

            if (Timing.CurTick > mover.LastInputTick)
            {
                walk = Vector2.Zero;
                sprint = Vector2.Zero;
                remainingFraction = 1;
            }
            else
            {
                walk = mover.CurTickWalkMovement;
                sprint = mover.CurTickSprintMovement;
                remainingFraction = (ushort.MaxValue - mover.LastInputSubTick) / (float) ushort.MaxValue;
            }

            var curDir = DirVecForButtons(mover.HeldMoveButtons) * remainingFraction;

            if (mover.Sprinting)
            {
                sprint += curDir;
            }
            else
            {
                walk += curDir;
            }

            // Logger.Info($"{curDir}{walk}{sprint}");
            return (walk, sprint);
        }

        /// <summary>
        ///     Toggles one of the four cardinal directions. Each of the four directions are
        ///     composed into a single direction vector, <see cref="VelocityDir"/>. Enabling
        ///     opposite directions will cancel each other out, resulting in no direction.
        /// </summary>
        public void SetVelocityDirection(InputMoverComponent component, Direction direction, ushort subTick, bool enabled)
        {
            // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] {direction}: {enabled}");

            var bit = direction switch
            {
                Direction.East => MoveButtons.Right,
                Direction.North => MoveButtons.Up,
                Direction.West => MoveButtons.Left,
                Direction.South => MoveButtons.Down,
                _ => throw new ArgumentException(nameof(direction))
            };

            SetMoveInput(component, subTick, enabled, bit);
        }

        private void SetMoveInput(InputMoverComponent component, ushort subTick, bool enabled, MoveButtons bit)
        {
            // Modifies held state of a movement button at a certain sub tick and updates current tick movement vectors.
            ResetSubtick(component);

            if (subTick >= component.LastInputSubTick)
            {
                var fraction = (subTick - component.LastInputSubTick) / (float) ushort.MaxValue;

                ref var lastMoveAmount = ref component.Sprinting ? ref component.CurTickSprintMovement : ref component.CurTickWalkMovement;

                lastMoveAmount += DirVecForButtons(component.HeldMoveButtons) * fraction;

                component.LastInputSubTick = subTick;
            }

            var buttons = component.HeldMoveButtons;

            if (enabled)
            {
                buttons |= bit;
            }
            else
            {
                buttons &= ~bit;
            }

            SetMoveInput(component, buttons);
        }

        private void ResetSubtick(InputMoverComponent component)
        {
            if (Timing.CurTick <= component.LastInputTick) return;

            component.CurTickWalkMovement = Vector2.Zero;
            component.CurTickSprintMovement = Vector2.Zero;
            component.LastInputTick = Timing.CurTick;
            component.LastInputSubTick = 0;
        }

        public void SetSprinting(InputMoverComponent component, ushort subTick, bool walking)
        {
            // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] Sprint: {enabled}");

            SetMoveInput(component, subTick, walking, MoveButtons.Walk);
        }

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

        private sealed class MoverDirInputCmdHandler : InputCmdHandler
        {
            private SharedMoverController _controller;
            private readonly Direction _dir;

            public MoverDirInputCmdHandler(SharedMoverController controller, Direction dir)
            {
                _controller = controller;
                _dir = dir;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

                _controller.HandleDirChange(session.AttachedEntity.Value, _dir, message.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }

        private sealed class WalkInputCmdHandler : InputCmdHandler
        {
            private SharedMoverController _controller;

            public WalkInputCmdHandler(SharedMoverController controller)
            {
                _controller = controller;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

                _controller.HandleRunChange(session.AttachedEntity.Value, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }

        [Serializable, NetSerializable]
        private sealed class InputMoverComponentState : ComponentState
        {
            public MoveButtons Buttons { get; }
            public readonly bool CanMove;

            public InputMoverComponentState(MoveButtons buttons, bool canMove)
            {
                Buttons = buttons;
                CanMove = canMove;
            }
        }

        private sealed class ShuttleInputCmdHandler : InputCmdHandler
        {
            private SharedMoverController _controller;
            private ShuttleButtons _button;

            public ShuttleInputCmdHandler(SharedMoverController controller, ShuttleButtons button)
            {
                _controller = controller;
                _button = button;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

                _controller.HandleShuttleInput(session.AttachedEntity.Value, _button, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }
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
    Walk = 16,
}

[Flags]
public enum ShuttleButtons : byte
{
    None = 0,
    StrafeUp = 1 << 0,
    StrafeDown = 1 << 1,
    StrafeLeft = 1 << 2,
    StrafeRight = 1 << 3,
    RotateLeft = 1 << 4,
    RotateRight = 1 << 5,
    Brake = 1 << 6,
}
