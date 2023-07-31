namespace Content.Shared.Weapons
{
    [RegisterComponent]
    public sealed class ResistancePenetrationComponent : Component
    {
        /// <summary>
        ///     The amount of the target's resistance that is negated
        ///     1 means complete resistance negation.
        /// </summary>
        [DataField("penetration")]
        public float? Penetration;
    }
}
