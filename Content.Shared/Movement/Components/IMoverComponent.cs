using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.Movement.Components
{
    // Does nothing except ensure uniqueness between mover components.
    // There can only be one.
    public interface IMoverComponent : IComponent
    {
        /// <summary>
        ///     Movement speed (m/s) that the entity walks.
        /// </summary>
        float CurrentWalkSpeed { get; }

        /// <summary>
        ///     Movement speed (m/s) that the entity sprints.
        /// </summary>
        float CurrentSprintSpeed { get; }

        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        bool Sprinting { get; }

        Angle LastGridAngle { get; set; }

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        (Vector2 walking, Vector2 sprinting) VelocityDir { get; }

        /// <summary>
        ///     Toggles one of the four cardinal directions. Each of the four directions are
        ///     composed into a single direction vector, <see cref="SharedPlayerInputMoverComponent.VelocityDir"/>. Enabling
        ///     opposite directions will cancel each other out, resulting in no direction.
        /// </summary>
        /// <param name="direction">Direction to toggle.</param>
        /// <param name="subTick"></param>
        /// <param name="enabled">If the direction is active.</param>
        void SetVelocityDirection(Direction direction, ushort subTick, bool enabled);

        void SetSprinting(ushort subTick, bool walking);
    }
}
