using Content.Shared.Forensics;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed partial class ForensicsComponent : Component
    {
        [DataField]
        public Dictionary<ForensicEvidence, HashSet<string>> Evidence = [];

        /// <summary>
        /// List of cleaning agents found on the entity, separate from actual evidence
        /// </summary>
        [DataField]
        public List<string> CleaningAgents = [];

        /// <summary>
        /// How close you must be to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField("cleanDistance")]
        public float CleanDistance = 1.5f;

        /// <summary>
        /// Can the DNA be cleaned off of this entity?
        /// e.g. you can wipe the DNA off of a knife, but not a cigarette
        /// </summary>
        [DataField("canDnaBeCleaned")]
        public bool CanDnaBeCleaned = true;
    }
}
