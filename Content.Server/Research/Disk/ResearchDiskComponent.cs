namespace Content.Server.Research.Disk
{
    [RegisterComponent]
    public sealed class ResearchDiskComponent : Component
    {
        [DataField("points")]
        public int Points = 0;
        [DataField("technology")]
        public string Technology = "";
    }
}
