using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Follower.Components;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
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
            SubscribeLocalEvent<InputMoverComponent, ComponentGetState>(OnMoverGetState);
            SubscribeLocalEvent<InputMoverComponent, ComponentHandleState>(OnMoverHandleState);
            SubscribeLocalEvent<InputMoverComponent, EntParentChangedMessage>(OnInputParentChange);

            SubscribeLocalEvent<AutoOrientComponent, EntParentChangedMessage>(OnAutoParentChange);

            SubscribeLocalEvent<FollowedComponent, EntParentChangedMessage>(OnFollowedParentChange);

            Subs.CVar(_configManager, CCVars.CameraRotationLocked, obj => CameraRotationLocked = obj, true);
            Subs.CVar(_configManager, CCVars.GameDiagonalMovement, value => DiagonalMovementEnabled = value, true);
        }

        /// <summary>
        /// Gets the buttons held with opposites cancelled out.
        /// </summary>
        public static MoveButtons GetNormalizedMovement(MoveButtons buttons)
        {
            var oldMovement = buttons;

            if ((oldMovement & (MoveButtons.Left | MoveButtons.Right)) == (MoveButtons.Left | MoveButtons.Right))
            {
                oldMovement &= ~MoveButtons.Left;
                oldMovement &= ~MoveButtons.Right;
            }

            if ((oldMovement & (MoveButtons.Up | MoveButtons.Down)) == (MoveButtons.Up | MoveButtons.Down))
            {
                oldMovement &= ~MoveButtons.Up;
                oldMovement &= ~MoveButtons.Down;
            }

            return oldMovement;
        }

        protected void SetMoveInput(InputMoverComponent component, MoveButtons buttons)
        {
            if (component.HeldMoveButtons == buttons)
                return;

            // Relay the fact we had any movement event.
            // TODO: Ideally we'd do these in a tick instead of out of sim.
            var moveEvent = new MoveInputEvent(component.Owner, component, component.HeldMoveButtons);
            component.HeldMoveButtons = buttons;
            RaiseLocalEvent(component.Owner, ref moveEvent);
            Dirty(component.Owner, component);
        }

        private void OnMoverHandleState(EntityUid uid, InputMoverComponent component, ComponentHandleState args)
        {
            if (args.Current is not InputMoverComponentState state)
                return;

            // Handle state
            component.LerpTarget = state.LerpTarget;
            component.RelativeRotation = state.RelativeRotation;
            component.TargetRelativeRotation = state.TargetRelativeRotation;
            component.CanMove = state.CanMove;
            component.RelativeEntity = EnsureEntity<InputMoverComponent>(state.RelativeEntity, uid);

            // Reset
            component.LastInputTick = GameTick.Zero;
            component.LastInputSubTick = 0;

            if (component.HeldMoveButtons != state.HeldMoveButtons)
            {
                var moveEvent = new MoveInputEvent(uid, component, component.HeldMoveButtons);
                component.HeldMoveButtons = state.HeldMoveButtons;
                RaiseLocalEvent(uid, ref moveEvent);
            }
        }

        private void OnMoverGetState(EntityUid uid, InputMoverComponent component, ref ComponentGetState args)
        {
            args.State = new InputMoverComponentState()
            {
                CanMove = component.CanMove,
                RelativeEntity = GetNetEntity(component.RelativeEntity),
                LerpTarget = component.LerpTarget,
                HeldMoveButtons = component.HeldMoveButtons,
                RelativeRotation = component.RelativeRotation,
                TargetRelativeRotation = component.TargetRelativeRotation,
            };
        }

        private void ShutdownInput()
        {
            CommandBinds.Unregister<SharedMoverController>();
        }

        public bool DiagonalMovementEnabled { get; private set; }

        protected virtual void HandleShuttleInput(EntityUid uid, ShuttleButtons button, ushort subTick, bool state) {}

        private void OnAutoParentChange(EntityUid uid, AutoOrientComponent component, ref EntParentChangedMessage args)
        {
            ResetCamera(uid);
        }

        public void RotateCamera(EntityUid uid, Angle angle)
        {
            if (CameraRotationLocked || !MoverQuery.TryGetComponent(uid, out var mover))
                return;

            mover.TargetRelativeRotation += angle;
            Dirty(uid, mover);
        }

        public void ResetCamera(EntityUid uid)
        {
            if (CameraRotationLocked ||
                !MoverQuery.TryGetComponent(uid, out var mover))
            {
                return;
            }

            // If we updated parent then cancel the accumulator and force it now.
            if (!TryUpdateRelative(mover, XformQuery.GetComponent(uid)) && mover.TargetRelativeRotation.Equals(Angle.Zero))
                return;

            mover.LerpTarget = TimeSpan.Zero;
            mover.TargetRelativeRotation = Angle.Zero;
            Dirty(uid, mover);
        }

        private bool TryUpdateRelative(InputMoverComponent mover, TransformComponent xform)
        {
            var relative = xform.GridUid;
            relative ??= xform.MapUid;

            // So essentially what we want:
            // 1. If we go from grid to map then preserve our rotation and continue as usual
            // 2. If we go from grid -> grid then (after lerp time) snap to nearest cardinal (probably imperceptible)
            // 3. If we go from map -> grid then (after lerp time) snap to nearest cardinal

            if (mover.RelativeEntity.Equals(relative))
                return false;

            // Okay need to get our old relative rotation with respect to our new relative rotation
            // e.g. if we were right side up on our current grid need to get what that is on our new grid.
            var currentRotation = Angle.Zero;
            var targetRotation = Angle.Zero;

            // Get our current relative rotation
            if (XformQuery.TryGetComponent(mover.RelativeEntity, out var oldRelativeXform))
            {
                currentRotation = _transform.GetWorldRotation(oldRelativeXform, XformQuery) + mover.RelativeRotation;
            }

            if (XformQuery.TryGetComponent(relative, out var relativeXform))
            {
                // This is our current rotation relative to our new parent.
                mover.RelativeRotation = (currentRotation - _transform.GetWorldRotation(relativeXform)).FlipPositive();
            }

            // If we went from grid -> map we'll preserve our worldrotation
            if (relative != null && _mapManager.IsMap(relative.Value))
            {
                targetRotation = currentRotation.FlipPositive().Reduced();
            }
            // If we went from grid -> grid OR grid -> map then snap the target to cardinal and lerp there.
            // OR just rotate to zero (depending on cvar)
            else if (relative != null && _mapManager.IsGrid(relative.Value))
            {
                if (CameraRotationLocked)
                    targetRotation = Angle.Zero;
                else
                    targetRotation = mover.RelativeRotation.GetCardinalDir().ToAngle().Reduced();
            }

            mover.RelativeEntity = relative;
            mover.TargetRelativeRotation = targetRotation;
            return true;
        }

        public Angle GetParentGridAngle(InputMoverComponent mover)
        {
            var rotation = mover.RelativeRotation;

            if (XformQuery.TryGetComponent(mover.RelativeEntity, out var relativeXform))
                return _transform.GetWorldRotation(relativeXform) + rotation;

            return rotation;
        }

        private void OnFollowedParentChange(EntityUid uid, FollowedComponent component, ref EntParentChangedMessage args)
        {
            foreach (var foll in component.Following)
            {
                if (!MoverQuery.TryGetComponent(foll, out var mover))
                    continue;

                var ev = new EntParentChangedMessage(foll, null, args.OldMapId, XformQuery.GetComponent(foll));
                OnInputParentChange(foll, mover, ref ev);
            }
        }

        private void OnInputParentChange(EntityUid uid, InputMoverComponent component, ref EntParentChangedMessage args)
        {
            // If we change our grid / map then delay updating our LastGridAngle.
            var relative = args.Transform.GridUid;
            relative ??= args.Transform.MapUid;

            if (component.LifeStage < ComponentLifeStage.Running)
            {
                component.RelativeEntity = relative;
                Dirty(uid, component);
                return;
            }

            var oldMapId = args.OldMapId;
            var mapId = args.Transform.MapUid;

            // If we change maps then reset eye rotation entirely.
            if (oldMapId != mapId)
            {
                component.RelativeEntity = relative;
                component.TargetRelativeRotation = Angle.Zero;
                component.RelativeRotation = Angle.Zero;
                component.LerpTarget = TimeSpan.Zero;
                Dirty(uid, component);
                return;
            }

            // If we go on a grid and back off then just reset the accumulator.
            if (relative == component.RelativeEntity)
            {
                if (component.LerpTarget >= Timing.CurTime)
                {
                    component.LerpTarget = TimeSpan.Zero;
                    Dirty(uid, component);
                }

                return;
            }

            component.LerpTarget = TimeSpan.FromSeconds(InputMoverComponent.LerpTime) + Timing.CurTime;
            Dirty(uid, component);
        }

        private void HandleDirChange(EntityUid entity, Direction dir, ushort subTick, bool state)
        {
            // Relayed movement just uses the same keybinds given we're moving the relayed entity
            // the same as us.

            if (TryComp<RelayInputMoverComponent>(entity, out var relayMover))
            {
                DebugTools.Assert(relayMover.RelayEntity != entity);
                DebugTools.AssertNotNull(relayMover.RelayEntity);

                if (MoverQuery.TryGetComponent(entity, out var mover))
                    SetMoveInput(mover, MoveButtons.None);

                if (!_mobState.IsIncapacitated(entity))
                    HandleDirChange(relayMover.RelayEntity, dir, subTick, state);

                return;
            }

            if (!MoverQuery.TryGetComponent(entity, out var moverComp))
                return;

            // For stuff like "Moving out of locker" or the likes
            // We'll relay a movement input to the parent.
            if (_container.IsEntityInContainer(entity) &&
                TryComp(entity, out TransformComponent? xform) &&
                xform.ParentUid.IsValid() &&
                _mobState.IsAlive(entity))
            {
                var relayMoveEvent = new ContainerRelayMovementEntityEvent(entity);
                RaiseLocalEvent(xform.ParentUid, ref relayMoveEvent);
            }

            SetVelocityDirection(entity, moverComp, dir, subTick, state);
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
            MoverQuery.TryGetComponent(uid, out var moverComp);

            if (TryComp<RelayInputMoverComponent>(uid, out var relayMover))
            {
                // if we swap to relay then stop our existing input if we ever change back.
                if (moverComp != null)
                {
                    SetMoveInput(moverComp, MoveButtons.None);
                }

                HandleRunChange(relayMover.RelayEntity, subTick, walking);
                return;
            }

            if (moverComp == null) return;

            SetSprinting(uid, moverComp, subTick, walking);
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
        public void SetVelocityDirection(EntityUid entity, InputMoverComponent component, Direction direction, ushort subTick, bool enabled)
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

            SetMoveInput(entity, component, subTick, enabled, bit);
        }

        private void SetMoveInput(EntityUid entity, InputMoverComponent component, ushort subTick, bool enabled, MoveButtons bit)
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

        public void SetSprinting(EntityUid entity, InputMoverComponent component, ushort subTick, bool walking)
        {
            // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] Sprint: {enabled}");

            SetMoveInput(entity, component, subTick, walking, MoveButtons.Walk);
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
            if (vec.LengthSquared() > 1.0e-6)
            {
                // Normalize so that diagonals aren't faster or something.
                vec = vec.Normalized();
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

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                if (session?.AttachedEntity == null) return false;

                if (message.State != BoundKeyState.Up)
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

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                if (session?.AttachedEntity == null) return false;

                if (message.State != BoundKeyState.Up)
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

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                if (session?.AttachedEntity == null) return false;

                _controller.HandleDirChange(session.AttachedEntity.Value, _dir, message.SubTick, message.State == BoundKeyState.Down);
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

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                if (session?.AttachedEntity == null) return false;

                _controller.HandleRunChange(session.AttachedEntity.Value, message.SubTick, message.State == BoundKeyState.Down);
                return false;
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

            public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
            {
                if (session?.AttachedEntity == null) return false;

                _controller.HandleShuttleInput(session.AttachedEntity.Value, _button, message.SubTick, message.State == BoundKeyState.Down);
                return false;
            }
        }
    }

    [Flags]
    [Serializable, NetSerializable]
    public enum MoveButtons : byte
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        Walk = 16,
        AnyDirection = Up | Down | Left | Right,
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
