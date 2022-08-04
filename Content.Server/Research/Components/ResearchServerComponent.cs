namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public sealed class ResearchServerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] public string ServerName => _serverName;

        [DataField("servername")]
        private string _serverName = "RDSERVER";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("points")]
        public int Points = 0;

        [ViewVariables(VVAccess.ReadOnly)] public int Id { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchPointSourceComponent> PointSources { get; } = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchClientComponent> Clients { get; } = new();
    }
}
