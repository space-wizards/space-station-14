using Content.Server.Cloning;

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
    }
}
