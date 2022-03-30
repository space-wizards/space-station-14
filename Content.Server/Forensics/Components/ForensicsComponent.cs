namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed class ForensicsComponent : Component
    {
        [DataField("fingerprints")]
        public HashSet<string> Fingerprints = new();

        [DataField("fibers")]
        public HashSet<string> Fibers = new();
    }
}
