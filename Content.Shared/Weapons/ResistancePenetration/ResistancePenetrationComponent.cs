namespace Content.Shared.Weapons
{
    [RegisterComponent]
    public sealed class ResistancePenetrationComponent : Component
    {
        /// <summary>
        ///     The amount of the target's resistance that is negated.
        ///     A value of 1 means all the resistances are ignored.
        /// </summary>
        [DataField("penetration")]
        public float? Penetration;
    }
}
