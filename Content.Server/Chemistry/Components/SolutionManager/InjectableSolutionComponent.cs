namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Denotes a solution which can be added with syringes.
    /// </summary>
    [RegisterComponent]
    public sealed partial class InjectableSolutionComponent : Component
    {

        /// <summary>
        /// Solution name which can be added with syringes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
