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

        [DataField("residues")]
        public HashSet<string> Residues = new();

        /// <summary>
        /// How long it takes to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField("cleanDelay")]
        public float CleanDelay = 12.0f;

        /// <summary>
        /// How close you must be to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField("cleanDistance")]
        public float CleanDistance = 1.5f;

        /// <summary>
        /// Can the DNA be cleaned off of this entity?
        /// e.g. you can clean the DNA off of a knife, but not a puddle
        /// </summary>
        [DataField("canDnaBeCleaned")]
        public bool CanDnaBeCleaned = true;
    }
}
