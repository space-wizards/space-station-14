namespace Content.Server.Disease.Components
{
    /// <summary>
    /// Value added to clothing to give its wearer
    /// protection against infection from diseases
    /// </summary>
    [RegisterComponent]
    public sealed class DiseaseProtectionComponent : Component
    {
        /// <summary>
        /// Float value between 0 and 1, will be subtracted
        /// from the infection chance (which is base 0.7)
        /// Reference guide is a full biosuit w/gloves & mask
        /// should add up to exactly 0.7
        /// </summary>
        [DataField("protection")]
        public float Protection = 0.1f;
        /// <summary>
        /// Is the component currently being worn and affecting someone's disease
        /// resistance? Making the unequip check not totally CBT
        /// </summary>
        public bool IsActive = false;
    }
}
