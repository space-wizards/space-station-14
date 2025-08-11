namespace Content.Server.Singularity.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SinguloFoodComponent : Component
    {
        /// <summary>
        /// Flat adjustment to the singularity's energy when this entity is eaten by the event horizon.
        /// </summary>
        [DataField]
        public float Energy = 1f;

        /// <summary>
        /// Multiplier applied to singularity's energy.
        /// 1.0 = no change, 0.97 = 3% reduction, 1.05 = 5% increase
        /// </summary>
        /// /// <remarks>
        /// This is calculated using the singularity's energy level before <see cref="Energy"/> has been added.
        /// </remarks>
        [DataField]
        public float EnergyFactor = 1f;
    }
}
