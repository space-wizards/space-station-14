namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Denotes the solution that can removed  be with syringes.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DrawableSolutionComponent : Component
    {
        /// <summary>
        /// Solution name that can be removed with syringes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
