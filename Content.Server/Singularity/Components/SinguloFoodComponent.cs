namespace Content.Server.Singularity.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SinguloFoodComponent : Component
    {
        [DataField("energy")]
        public float Energy = 1f;

        /// <summary>
        /// The percentage of the singularity's current energy that this food will drain.
        /// Only applies when Energy is negative.
        /// </summary>
        [DataField("percentageDrain")]
        public float PercentageDrain = -0.03f;
    }
}
