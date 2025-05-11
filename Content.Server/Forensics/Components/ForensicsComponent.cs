using Content.Shared.Forensics;
using Robust.Shared.Prototypes;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed partial class ForensicsComponent : Component
    {
        /// <summary>
        /// A dictionary of types of evidence to a unique collection of strings for it.
        /// </summary>
        [DataField]
        public Dictionary<ProtoId<ForensicEvidencePrototype>, HashSet<string>> Evidence = [];

        /// <summary>
        /// Unique set of cleaning agents found on the entity, separate from actual evidence
        /// </summary>
        [DataField]
        public HashSet<string> CleaningAgents = [];

        /// <summary>
        /// How close you must be to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField("cleanDistance")]
        public float CleanDistance = 1.5f;

        /// <summary>
        /// List of evidence that CANNOT be cleaned from this entity.
        /// </summary>
        [DataField]
        public List<ProtoId<ForensicEvidencePrototype>> CleanBlacklist = [];
    }
}
