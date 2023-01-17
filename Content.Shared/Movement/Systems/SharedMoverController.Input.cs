using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems
{
    /// <summary>
    ///     Handles converting inputs into movement.
    /// </summary>
    public abstract partial class SharedMoverController
    {
        public bool CameraRotationLocked { get; set; }

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
                .Bind(EngineKeyFunctions.CameraRotateLeft, new CameraRotateInputCmdHandler(this, Direction.East))
                .Bind(EngineKeyFunctions.CameraRotateRight, new CameraRotateInputCmdHandler(this, Direction.West))
                .Bind(EngineKeyFunctions.CameraReset, new CameraResetInputCmdHandler(this))
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
            SubscribeLocalEvent<InputMoverComponent, EntParentChangedMessage>(OnInputParentChange);

            _configManager.OnValueChanged(CCVars.CameraRotationLocked, SetCameraRotationLocked, true);
            _configManager.OnValueChanged(CCVars.GameDiagonalMovement, SetDiagonalMovement, true);
        }

        private void SetCameraRotationLocked(bool obj)
        {
            CameraRotationLocked = obj;
        }

        protected void SetMoveInput(InputMoverComponent component, MoveButtons buttons)
        {
            if (component.HeldMoveButtons == buttons) return;
            component.HeldMoveButtons = buttons;
            Dirty(component);
        }

        private void OnInputHandleState(EntityUid uid, InputMoverComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not InputMoverComponentState state)
                return;

            component.HeldMoveButtons = state.Buttons;
            component.LastInputTick = GameTick.Zero;
            component.LastInputSubTick = 0;
            component.CanMove = state.CanMove;

            component.RelativeRotation = state.RelativeRotation;
            component.TargetRelativeRotation = state.TargetRelativeRotation;
            component.RelativeEntity = state.RelativeEntity;
            component.LerpAccumulator = state.LerpAccumulator;
        }

        private void OnInputGetState(EntityUid uid, InputMoverComponent component, ref ComponentGetState args)
        {
            args.State = new InputMoverComponentState(
                component.HeldMoveButtons,
                component.CanMove,
                component.RelativeRotation,
                component.TargetRelativeRotation,
                component.RelativeEntity,
                component.LerpAccumulator);
        }

        private void ShutdownInput()
        {
            CommandBinds.Unregister<SharedMoverController>();
            _configManager.UnsubValueChanged(CCVars.CameraRotationLocked, SetCameraRotationLocked);
            _configManager.UnsubValueChanged(CCVars.GameDiagonalMovement, SetDiagonalMovement);
        }

        public bool DiagonalMovementEnabled { get; private set; }

        private void SetDiagonalMovement(bool value) => DiagonalMovementEnabled = value;

        protected virtual void HandleShuttleInput(EntityUid uid, ShuttleButtons button, ushort subTick, bool state) {}

        public void RotateCamera(EntityUid uid, Angle angle)
        {
            if (CameraRotationLocked || !TryComp<InputMoverComponent>(uid, out var mover))
                return;

            mover.TargetRelativeRotation += angle;
            Dirty(mover);
        }

        public void ResetCamera(EntityUid uid)
        {
            if (CameraRotationLocked || !TryComp<InputMoverComponent>(uid, out var mover) || mover.TargetRelativeRotation.Equals(Angle.Zero))
                return;

            mover.TargetRelativeRotation = Angle.Zero;
            Dirty(mover);
        }

        public Angle GetParentGridAngle(InputMoverComponent mover, EntityQuery<TransformComponent> xformQuery)
        {
            var rotation = mover.RelativeRotation;

            if (xformQuery.TryGetComponent(mover.RelativeEntity, out var relativeXform))
                return (_transform.GetWorldRotation(relativeXform, xformQuery) + rotation);

            return rotation;
        }

        public Angle GetParentGridAngle(InputMoverComponent mover)
        {
            var rotation = mover.RelativeRotation;

            if (TryComp<TransformComponent>(mover.RelativeEntity, out var relativeXform))
                return (relativeXform.WorldRotation + rotation);

            return rotation;
        }

        private void OnInputParentChange(EntityUid uid, InputMoverComponent component, ref EntParentChangedMessage args)
        {
            // If we change our grid / map then delay updating our LastGridAngle.
            var relative = args.Transform.GridUid;
            relative ??= args.Transform.MapUid;

            if (component.LifeStage < ComponentLifeStage.Running)
            {
                component.RelativeEntity = relative;
                Dirty(component);
                return;
            }

            var oldMapId = args.OldMapId;
            var mapId = args.Transform.MapID;

            // If we change maps then reset eye rotation entirely.
            if (oldMapId != mapId)
            {
                component.RelativeEntity = relative;
                component.TargetRelativeRotation = Angle.Zero;
                component.RelativeRotation = Angle.Zero;
                component.LerpAccumulator = 0f;
                Dirty(component);
                return;
            }

            // If we go on a grid and back off then just reset the accumulator.
            if (relative == component.RelativeEntity)
            {
                if (component.LerpAccumulator != 0f)
                {
                    component.LerpAccumulator = 0f;
                    Dirty(component);
                }

                return;
            }

            component.LerpAccumulator = InputMoverComponent.LerpTime;
            Dirty(component);
        }

        private void HandleDirChange(EntityUid entity, Direction dir, ushort subTick, bool state)
        {
            // Relayed movement just uses the same keybinds given we're moving the relayed entity
            // the same as us.

            if (TryComp<RelayInputMoverComponent>(entity, out var relayMover))
            {
                DebugTools.Assert(relayMover.RelayEntity != entity);
                DebugTools.AssertNotNull(relayMover.RelayEntity);

                if (TryComp<InputMoverComponent>(entity, out var mover))
                    SetMoveInput(mover, MoveButtons.None);

                DebugTools.Assert(TryComp(relayMover.RelayEntity, out MovementRelayTargetComponent? targetComp) && targetComp.Entities.Count == 1,
                    "Multiple relayed movers are not supported at the moment");

                if (relayMover.RelayEntity != null && !_mobState.IsIncapacitated(entity))
                    HandleDirChange(relayMover.RelayEntity.Value, dir, subTick, state);

                return;
            }

            if (!TryComp<InputMoverComponent>(entity, out var moverComp))
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

            if (!xform.ParentUid.IsValid())
                return;

            component.RelativeEntity = xform.GridUid ?? xform.MapUid;
            component.TargetRelativeRotation = Angle.Zero;
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

        private sealed class CameraRotateInputCmdHandler : InputCmdHandler
        {
            private readonly SharedMoverController _controller;
            private readonly Angle _angle;

            public CameraRotateInputCmdHandler(SharedMoverController controller, Direction direction)
            {
                _controller = controller;
                _angle = direction.ToAngle();
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

                if (full.State != BoundKeyState.Up)
                    return false;

                _controller.RotateCamera(session.AttachedEntity.Value, _angle);
                return false;
            }
        }

        private sealed class CameraResetInputCmdHandler : InputCmdHandler
        {
            private readonly SharedMoverController _controller;

            public CameraResetInputCmdHandler(SharedMoverController controller)
            {
                _controller = controller;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

                if (full.State != BoundKeyState.Up)
                    return false;

                _controller.ResetCamera(session.AttachedEntity.Value);
                return false;
            }
        }

        private sealed class MoverDirInputCmdHandler : InputCmdHandler
        {
            private readonly SharedMoverController _controller;
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

            /// <summary>
            /// Our current rotation for movement purposes. This is lerping towards <see cref="TargetRelativeRotation"/>
            /// </summary>
            public Angle RelativeRotation;

            /// <summary>
            /// Target rotation relative to the <see cref="RelativeEntity"/>. Typically 0
            /// </summary>
            public Angle TargetRelativeRotation;
            public EntityUid? RelativeEntity;
            public float LerpAccumulator = 0f;

            public InputMoverComponentState(MoveButtons buttons, bool canMove, Angle relativeRotation, Angle targetRelativeRotation, EntityUid? relativeEntity, float lerpAccumulator)
            {
                Buttons = buttons;
                CanMove = canMove;
                RelativeRotation = relativeRotation;
                TargetRelativeRotation = targetRelativeRotation;
                RelativeEntity = relativeEntity;
                LerpAccumulator = lerpAccumulator;
            }
        }

        private sealed class ShuttleInputCmdHandler : InputCmdHandler
        {
            private readonly SharedMoverController _controller;
            private readonly ShuttleButtons _button;

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

}
