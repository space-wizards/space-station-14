using Content.Shared.CCVar;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
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
                .Register<SharedMoverController>();

            SubscribeLocalEvent<InputMoverComponent, ComponentInit>(OnInputInit);
            SubscribeLocalEvent<InputMoverComponent, ComponentGetState>(OnInputGetState);
            SubscribeLocalEvent<InputMoverComponent, ComponentHandleState>(OnInputHandleState);
        }

        private void OnInputHandleState(EntityUid uid, InputMoverComponent component, ref ComponentHandleState args)
        {
            if (args.Current is InputMoverComponentState state)
            {
                component.HeldMoveButtons = state.Buttons;
                component.LastInputTick = GameTick.Zero;
                component.LastInputSubTick = 0;
                component.CanMove = state.CanMove;
            }
        }

        private void OnInputGetState(EntityUid uid, InputMoverComponent component, ref ComponentGetState args)
        {
            args.State = new InputMoverComponentState(component.HeldMoveButtons, component.CanMove);
        }

        private void ShutdownInput()
        {
            CommandBinds.Unregister<SharedMoverController>();
        }

        public bool DiagonalMovementEnabled => _configManager.GetCVar<bool>(CCVars.GameDiagonalMovement);

        private void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
        {
            if (!TryComp<InputMoverComponent>(session?.AttachedEntity, out var moverComp))
                return;

            var owner = session?.AttachedEntity;

            if (owner != null && session != null)
            {
                EntityManager.EventBus.RaiseLocalEvent(owner.Value, new RelayMoveInputEvent(session), true);

                // For stuff like "Moving out of locker" or the likes
                if (owner.Value.IsInContainer() &&
                    (!EntityManager.TryGetComponent(owner.Value, out MobStateComponent? mobState) ||
                     mobState.IsAlive()))
                {
                    var relayMoveEvent = new RelayMovementEntityEvent(owner.Value);
                    EntityManager.EventBus.RaiseLocalEvent(EntityManager.GetComponent<TransformComponent>(owner.Value).ParentUid, relayMoveEvent, true);
                }
                // Pass the rider's inputs to the vehicle (the rider itself is on the ignored list in C.S/MoverController.cs)
                if (TryComp<RiderComponent>(owner.Value, out var rider) && rider.Vehicle != null && rider.Vehicle.HasKey)
                {
                    if (TryComp<InputMoverComponent>(rider.Vehicle.Owner, out var vehicleMover))
                    {
                        SetVelocityDirection(vehicleMover, dir, subTick, state);
                    }
                }
            }

            SetVelocityDirection(moverComp, dir, subTick, state);
        }

        private void OnInputInit(EntityUid uid, InputMoverComponent component, ComponentInit args)
        {
            var xform = Transform(uid);

            if (!xform.ParentUid.IsValid()) return;

            component.LastGridAngle = Transform(xform.ParentUid).WorldRotation;
        }

        private void HandleRunChange(ICommonSession? session, ushort subTick, bool walking)
        {
            if (!TryComp<InputMoverComponent>(session?.AttachedEntity, out var moverComp))
            {
                return;
            }

            SetSprinting(moverComp, subTick, walking);
        }

        public (Vector2 Walking, Vector2 Sprinting) GetVelocityInput(InputMoverComponent mover)
        {
            if (!_timing.InSimulation)
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

            if (_timing.CurTick > mover.LastInputTick)
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

            if (_timing.CurTick > component.LastInputTick)
            {
                component.CurTickWalkMovement = Vector2.Zero;
                component.CurTickSprintMovement = Vector2.Zero;
                component.LastInputTick = _timing.CurTick;
                component.LastInputSubTick = 0;
            }

            if (subTick >= component.LastInputSubTick)
            {
                var fraction = (subTick - component.LastInputSubTick) / (float) ushort.MaxValue;

                ref var lastMoveAmount = ref component.Sprinting ? ref component.CurTickSprintMovement : ref component.CurTickWalkMovement;

                lastMoveAmount += DirVecForButtons(component.HeldMoveButtons) * fraction;

                component.LastInputSubTick = subTick;
            }

            if (enabled)
            {
                component.HeldMoveButtons |= bit;
            }
            else
            {
                component.HeldMoveButtons &= ~bit;
            }

            Dirty(component);
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
                if (message is not FullInputCmdMessage full) return false;

                _controller.HandleDirChange(session, _dir, message.SubTick, full.State == BoundKeyState.Down);
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
                if (message is not FullInputCmdMessage full) return false;

                _controller.HandleRunChange(session, full.SubTick, full.State == BoundKeyState.Down);
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
