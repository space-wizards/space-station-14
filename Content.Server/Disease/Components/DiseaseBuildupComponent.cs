namespace Content.Server.Disease.Components
{
    /// <summary>
    /// The component which records the buildup/progression of an infection
    /// </summary>
    [RegisterComponent]
    public sealed class DiseaseBuildupComponent : Component
    {
        /// This could be served to be generalized to allow for multiple
        /// diseases to build up at once, but it doesn't matter too much.

        /// <summary>
        /// The current amount of progression that has built up.
        /// </summary>
        [DataField("progression")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Progression = 0.00f;
    }
}
