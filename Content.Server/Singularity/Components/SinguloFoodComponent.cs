namespace Content.Server.Singularity.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SinguloFoodComponent : Component
    {
        [DataField]
        public float Energy = 1f;

        /// <summary>
        /// Multiplier applied to singularity's energy.
        /// 1.0 = no change, 0.97 = 3% reduction, 1.05 = 5% increase
        /// </summary>
        [DataField]
        public float EnergyFactor = 1f;
    }
}
