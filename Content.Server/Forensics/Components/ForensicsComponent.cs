namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed partial class ForensicsComponent : Component
    {
        [DataField("fingerprints")]
        public HashSet<string> Fingerprints = new();

        [DataField("fibers")]
        public HashSet<string> Fibers = new();

        [DataField("dnas")]
        public HashSet<string> DNAs = new();
    }
}
