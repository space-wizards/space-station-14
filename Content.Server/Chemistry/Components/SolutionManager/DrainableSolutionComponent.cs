namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Denotes the solution that can be easily removed through any reagent container.
    ///     Think pouring this or draining from a water tank.
    /// </summary>
    [RegisterComponent]
    public sealed class DrainableSolutionComponent : Component
    {
        /// <summary>
        /// Solution name that can be drained.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
