namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    public sealed class ExaminableSolutionComponent: Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
