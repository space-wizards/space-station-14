using Content.Shared.Physics;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server.Paper.Plane
{
    [RegisterComponent]
    public sealed class PaperPlaneComponent : Component
    {
        /// <summary>
        /// Delay for unfolding this plane back into paper
        /// </summary>
        [DataField("unoldDelay")]
        public float UnfoldDelay = 1.0f;

        /// <summary>
        /// Tags to apply to the paper when in plane form
        /// </summary>
        [DataField("tags")]
        public string[] Tags = { "NoSpinOnThrow", "Trash" };

        /// <summary>
        /// Collision mask for in-flight, overriding ThrownItem
        /// </summary>
        public readonly CollisionGroup CollisionMask = CollisionGroup.ItemMask;

        public CancellationTokenSource? CancelToken = null;
    }
}
