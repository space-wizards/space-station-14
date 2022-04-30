using Content.Server.Cloning;
using Content.Shared.Body.Components;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class SkeletonBodyManagerComponent : Component
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
        public float DoAfterTime = 5f;

        [ViewVariables]
        public List<SharedBodyPartComponent>? BodyParts = null;
    }
}
