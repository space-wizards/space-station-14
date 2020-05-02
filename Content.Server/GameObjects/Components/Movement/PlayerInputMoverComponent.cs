
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Configuration;

namespace Content.Server.GameObjects.Components.Movement
{
    /// <summary>
    ///     Moves the entity based on input from a KeyBindingInputComponent.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class PlayerInputMoverComponent : Component, IMoverComponent, ICollideSpecial
    {
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configurationManager;
#pragma warning restore 649

        private bool _movingUp;
        private bool _movingDown;
        private bool _movingLeft;
        private bool _movingRight;

        /// <inheritdoc />
        public override string Name => "PlayerInputMover";

        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables]
        public float CurrentWalkSpeed
        {
            get
            {
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent component))
                {
                    return component.CurrentWalkSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            }
        }
        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables]
        public float CurrentSprintSpeed
        {
            get
            {
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent component))
                {
                    return component.CurrentSprintSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            }
        }

        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        [ViewVariables]
        public bool Sprinting { get; set; } = true;

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 VelocityDir { get; private set; }

        public GridCoordinates LastPosition { get; set; }

        public float StepSoundDistance { get; set; }

        /// <summary>
        ///     Whether or not the player can move diagonally.
        /// </summary>
        [ViewVariables] public bool DiagonalMovementEnabled => _configurationManager.GetCVar<bool>("game.diagonalmovement");

        /// <inheritdoc />
        public override void OnAdd()
        {
            // This component requires that the entity has a PhysicsComponent.
            if (!Owner.HasComponent<PhysicsComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(PhysicsComponent)}. ");

            base.OnAdd();
        }

        /// <summary>
        ///     Toggles one of the four cardinal directions. Each of the four directions are
        ///     composed into a single direction vector, <see cref="VelocityDir"/>. Enabling
        ///     opposite directions will cancel each other out, resulting in no direction.
        /// </summary>
        /// <param name="direction">Direction to toggle.</param>
        /// <param name="enabled">If the direction is active.</param>
        public void SetVelocityDirection(Direction direction, bool enabled)
        {
            switch (direction)
            {
                case Direction.East:
                    _movingRight = enabled;
                    break;
                case Direction.North:
                    _movingUp = enabled;
                    break;
                case Direction.West:
                    _movingLeft = enabled;
                    break;
                case Direction.South:
                    _movingDown = enabled;
                    break;
            }

            // key directions are in screen coordinates
            // _moveDir is in world coordinates
            // if the camera is moved, this needs to be changed

            var x = 0;
            x -= _movingLeft ? 1 : 0;
            x += _movingRight ? 1 : 0;

            var y = 0;
            if (DiagonalMovementEnabled || x == 0)
            {
                y -= _movingDown ? 1 : 0;
                y += _movingUp ? 1 : 0;
            }

            VelocityDir = new Vector2(x, y);

            // can't normalize zero length vector
            if (VelocityDir.LengthSquared > 1.0e-6)
                VelocityDir = VelocityDir.Normalized;
        }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            // Don't collide with other mobs
            if (collidedwith.Owner.TryGetComponent<SpeciesComponent>(out var collidedSpeciesComponent))
            {
                return true;
            }
            return false;
        }

    }
}
