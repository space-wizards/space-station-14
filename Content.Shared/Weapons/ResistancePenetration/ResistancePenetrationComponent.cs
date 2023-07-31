namespace Content.Shared.Weapons
{
    [RegisterComponent]
    public sealed class ResistancePenetrationComponent : Component
    {
        /// summary
        /// Target's resistance gets reduced by this amount,
        /// 1 means complete resistance negation.
        /// /summary
        [DataField("penetration")]
        public float? Penetration;
    }
}
