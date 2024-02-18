namespace Content.Shared.CombatMode
{
    public sealed class DisarmedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The entity being disarmed.
        /// </summary>
        public EntityUid Target { get; init; }

        /// <summary>
        ///     The entity performing the disarm.
        /// </summary>
        public EntityUid Source { get; init; }

        /// <summary>
        ///     Probability for push/knockdown.
        /// </summary>
        public float PushProbability { get; init; }

        /// <summary>
        ///     Whether the entity was successfully disarmed.
        /// </summary>
        public bool IsDisarmed { get; set; }

        /// <summary>
        ///     Whether the entity was successfully shoved.
        /// </summary>
        public bool IsShoved { get; set; }

        /// <summary>
        ///     Whether the entity was successfully stunned from a shove.
        /// </summary>
        public bool IsStunned { get; set; }
    }
}
