using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

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
    [Access(typeof(SharedSubFloorHideSystem))]
    public sealed partial class SubFloorHideComponent : Component
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
        /// Whether this entity's ambience should be disabled when underneath the floor.
        /// </summary>
        /// <remarks>
        /// Useful for cables and piping, gives maint it's distinct noise.
        /// </remarks>
        [DataField("blockAmbience")]
        public bool BlockAmbience { get; set; } = true;

        /// <summary>
        ///     Sprite layer keys for the layers that are always visible, even if the entity is below a floor tile. E.g.,
        ///     the vent part of a vent is always visible, even though the piping is hidden.
        /// </summary>
        [DataField("visibleLayers")]
        public HashSet<Enum> VisibleLayers = new() { SubfloorLayers.FirstLayer };
    }
}
