namespace Content.Server.Research.Components
{
    [RegisterComponent]
    [Virtual]
    public class ResearchClientComponent : Component
    {
        public bool ConnectedToServer => Server != null;

        [ViewVariables(VVAccess.ReadOnly)]
        public ResearchServerComponent? Server { get; set; }
    }
}
