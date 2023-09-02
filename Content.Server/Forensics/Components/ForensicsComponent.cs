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
        
        /// <summary>
        ///     Chance of transfering DNA from this entity to spawned one after this this being destroyed
        /// </summary>
        [DataField("dnaTransferChanceFromDestroyed")]
        public float DNATransferChanceAfterDestroy = 0f;

        /// <summary>
        ///     Chance of transfering fibers and fingerprints from this entity to spawned one after this this being destroyed
        /// </summary>
        [DataField("restOfTransferChanceFromDestroyed")]
        public float RestOfTransferChanceAfterDestroy = 0f;
    }
}
