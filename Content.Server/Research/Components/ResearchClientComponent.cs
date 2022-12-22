namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public sealed class ResearchClientComponent : Component
    {
        public bool ConnectedToServer => Server != null;

        [ViewVariables(VVAccess.ReadOnly)]
        public ResearchServerComponent? Server { get; set; }
    }
}
