using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor
{
    /// <summary>
    /// Simple component that automatically hides the sibling
    /// <see cref="SpriteComponent" /> when the tile it's on is not a sub floor
    /// (plating).
    /// </summary>
    /// <seealso cref="P:Content.Shared.Maps.ContentTileDefinition.IsSubFloor" />
    [NetworkedComponent]
    [RegisterComponent]
    [Friend(typeof(SharedSubFloorHideSystem))]
    public sealed class SubFloorHideComponent : Component
    {
        /// <summary>
        ///     Whether the entity's current position has a "Floor-type" tile above its current position.
        /// </summary>
        [ViewVariables]
        public bool IsUnderCover { get; set; } = false;

        /// <summary>
        ///     Whether interactions with this entity should be blocked while it is under floor tiles.
        /// </summary>
        /// <remarks>
        ///     Useful for entities like vents, which are only partially hidden. Anchor attempts will still be blocked.
        /// </remarks>
        [DataField("blockInteractions")]
        public bool BlockInteractions { get; set; } = true;

        /// <summary>
        ///     When revealed using some scanning tool, what transparency should be used to draw this item?
        /// </summary>
        [DataField("scannerTransparency")]
        public float ScannerTransparency = 0.8f;

        /// <summary>
        ///     The entities this subfloor is revealed by.
        /// </summary>
        [ViewVariables]
        public HashSet<EntityUid> RevealedBy { get; set; } = new();
    }
}
