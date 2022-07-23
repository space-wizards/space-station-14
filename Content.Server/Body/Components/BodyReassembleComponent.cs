using System.Threading;
using Content.Server.Cloning;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class BodyReassembleComponent : Component
    {
        /// <summary>
        /// The dna entry used for reassembling the skeleton
        /// updated before the entity is gibbed.
        /// </summary>
        [ViewVariables]
        public ClonerDNAEntry? DNA = null;

        /// <summary>
        /// The default time it takes to reassemble itself
        /// </summary>
        [ViewVariables]
        [DataField("delay")]
        public float DoAfterTime = 5f;

        /// <summary>
        /// The list of body parts that are needed for reassembly
        /// </summary>
        [ViewVariables]
        public HashSet<EntityUid>? BodyParts = null;

        [DataField("action")]
        public InstantAction? ReassembleAction = null;

        public CancellationTokenSource? CancelToken = null;
    }
}
