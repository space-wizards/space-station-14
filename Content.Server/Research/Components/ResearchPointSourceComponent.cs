namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public sealed class ResearchPointSourceComponent : ResearchClientComponent
    {
        [DataField("pointspersecond")]
        private int _pointsPerSecond;
        [DataField("active")]
        private bool _active;
        [ViewVariables(VVAccess.ReadWrite)]
        public int PointsPerSecond
        {
            get => _pointsPerSecond;
            set => _pointsPerSecond = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Active
        {
            get => _active;
            set => _active = value;
        }
    }
}
