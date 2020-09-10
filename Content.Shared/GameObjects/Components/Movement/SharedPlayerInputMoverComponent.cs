#nullable enable
using System;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Rotation;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedPlayerInputMoverComponent : Component, IMoverComponent, ICollideSpecial
    {
        // This class has to be able to handle server TPS being lower than client FPS.
        // While still having perfectly responsive movement client side.
        // We do this by keeping track of the exact sub-tick values that inputs are pressed on the client,
        // and then building a total movement vector based on those sub-tick steps.
        //
        // We keep track of the last sub-tick a movement input came in,
        // Then when a new input comes in, we calculate the fraction of the tick the LAST input was active for
        //   (new sub-tick - last sub-tick)
        // and then add to the total-this-tick movement vector
        // by multiplying that fraction by the movement direction for the last input.
        // This allows us to incrementally build the movement vector for the current tick,
        // without having to keep track of some kind of list of inputs and calculating it later.
        //
        // We have to keep track of a separate movement vector for walking and sprinting,
        // since we don't actually know our current movement speed while processing inputs.
        // We change which vector we write into based on whether we were sprinting after the previous input.
        //   (well maybe we do but the code is designed such that MoverSystem applies movement speed)
        //   (and I'm not changing that)

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public sealed override string Name => "PlayerInputMover";
        public sealed override uint? NetID => ContentNetIDs.PLAYER_INPUT_MOVER;

        private GameTick _lastInputTick;
        private ushort _lastInputSubTick;
        private Vector2 _curTickWalkMovement;
        private Vector2 _curTickSprintMovement;

        private MoveButtons _heldMoveButtons = MoveButtons.None;

        public float CurrentWalkSpeed
        {
            get
            {
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent? component))
                {
                    return component.CurrentWalkSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            }
        }

        public float CurrentSprintSpeed
        {
            get
            {
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent? component))
                {
                    return component.CurrentSprintSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentPushSpeed => 5;
        [ViewVariables(VVAccess.ReadWrite)]
        public float GrabRange => 0.2f;
        public bool Sprinting => !HasFlag(_heldMoveButtons, MoveButtons.Walk);

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public (Vector2 walking, Vector2 sprinting) VelocityDir
        {
            get
            {
                if (!_gameTiming.InSimulation)
                {
                    // Outside of simulation we'll be running client predicted movement per-frame.
                    // So return a full-length vector as if it's a full tick.
                    // Physics system will have the correct time step anyways.
                    var immediateDir = DirVecForButtons(_heldMoveButtons);
                    return Sprinting ? (Vector2.Zero, immediateDir) : (immediateDir, Vector2.Zero);
                }

                Vector2 walk;
                Vector2 sprint;
                float remainingFraction;

                if (_gameTiming.CurTick > _lastInputTick)
                {
                    walk = Vector2.Zero;
                    sprint = Vector2.Zero;
                    remainingFraction = 1;
                }
                else
                {
                    walk = _curTickWalkMovement;
                    sprint = _curTickSprintMovement;
                    remainingFraction = (ushort.MaxValue - _lastInputSubTick) / (float) ushort.MaxValue;
                }

                var curDir = DirVecForButtons(_heldMoveButtons) * remainingFraction;

                if (Sprinting)
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
        }

        public abstract EntityCoordinates LastPosition { get; set; }
        public abstract float StepSoundDistance { get; set; }

        /// <summary>
        ///     Whether or not the player can move diagonally.
        /// </summary>
        [ViewVariables]
        public bool DiagonalMovementEnabled => _configurationManager.GetCVar<bool>("game.diagonalmovement");

        /// <inheritdoc />
        public override void OnAdd()
        {
            // This component requires that the entity has a CollidableComponent.
            if (!Owner.HasComponent<ICollidableComponent>())
                Logger.Error(
                    $"[ECS] {Owner.Prototype?.Name} - {nameof(SharedPlayerInputMoverComponent)} requires" +
                    $" {nameof(ICollidableComponent)}. ");

            base.OnAdd();
        }


        /// <summary>
        ///     Toggles one of the four cardinal directions. Each of the four directions are
        ///     composed into a single direction vector, <see cref="VelocityDir"/>. Enabling
        ///     opposite directions will cancel each other out, resulting in no direction.
        /// </summary>
        /// <param name="direction">Direction to toggle.</param>
        /// <param name="subTick"></param>
        /// <param name="enabled">If the direction is active.</param>
        public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
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

            SetMoveInput(subTick, enabled, bit);
        }

        private void SetMoveInput(ushort subTick, bool enabled, MoveButtons bit)
        {
            // Modifies held state of a movement button at a certain sub tick and updates current tick movement vectors.

            if (_gameTiming.CurTick > _lastInputTick)
            {
                _curTickWalkMovement = Vector2.Zero;
                _curTickSprintMovement = Vector2.Zero;
                _lastInputTick = _gameTiming.CurTick;
                _lastInputSubTick = 0;
            }

            if (subTick >= _lastInputSubTick)
            {
                var fraction = (subTick - _lastInputSubTick) / (float) ushort.MaxValue;

                ref var lastMoveAmount = ref Sprinting ? ref _curTickSprintMovement : ref _curTickWalkMovement;

                lastMoveAmount += DirVecForButtons(_heldMoveButtons) * fraction;

                _lastInputSubTick = subTick;
            }

            if (enabled)
            {
                _heldMoveButtons |= bit;
            }
            else
            {
                _heldMoveButtons &= ~bit;
            }

            Dirty();
        }

        public void SetSprinting(ushort subTick, bool walking)
        {
            // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] Sprint: {enabled}");

            SetMoveInput(subTick, walking, MoveButtons.Walk);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is MoverComponentState state)
            {
                _heldMoveButtons = state.Buttons;
                _lastInputTick = GameTick.Zero;
                _lastInputSubTick = 0;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new MoverComponentState(_heldMoveButtons);
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

        bool ICollideSpecial.PreventCollide(IPhysBody collidedWith)
        {
            // Don't collide with other mobs
            return collidedWith.Entity.HasComponent<ISharedBodyManagerComponent>();
        }

        [Serializable, NetSerializable]
        private sealed class MoverComponentState : ComponentState
        {
            public MoveButtons Buttons { get; }

            public MoverComponentState(MoveButtons buttons) : base(ContentNetIDs
                .PLAYER_INPUT_MOVER)
            {
                Buttons = buttons;
            }
        }

        [Flags]
        private enum MoveButtons : byte
        {
            None = 0,
            Up = 1,
            Down = 2,
            Left = 4,
            Right = 8,
            Walk = 16,
        }

        private static bool HasFlag(MoveButtons buttons, MoveButtons flag)
        {
            return (buttons & flag) == flag;
        }
    }
}
