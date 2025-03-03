namespace Content.Server.Singularity.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SinguloFoodComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        public float Energy { get; set; } = 1f;

        /// <summary>
        /// The percentage of the singularity's current energy that this food will drain.
        /// Only applies when Energy is negative.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("percentageDrain")]
        public float PercentageDrain { get; set; }
    }
}
