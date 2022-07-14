namespace Content.Shared.Movement.Components
{
    // Does nothing except ensure uniqueness between mover components.
    // There can only be one.
    public interface IMoverComponent : IComponent
    {
        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        bool Sprinting { get; }

        /// <summary>
        ///     Can the entity currently move. Avoids having to raise move-attempt events every time a player moves.
        ///     Note that this value will be overridden by the action blocker system, and shouldn't just be set directly.
        /// </summary>
        bool CanMove { get; set; }

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
