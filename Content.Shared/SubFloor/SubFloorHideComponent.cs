using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor
{
    /// <summary>
    /// Simple component that automatically hides the sibling
    /// <see cref="ISpriteComponent" /> when the tile it's on is not a sub floor
    /// (plating).
    /// </summary>
    /// <seealso cref="P:Content.Shared.Maps.ContentTileDefinition.IsSubFloor" />
    [NetworkedComponent]
    [RegisterComponent]
    [Friend(typeof(SubFloorHideSystem))]
    public sealed class SubFloorHideComponent : Component
    {
        /// <summary>
        ///     Whether the entity will be hid when not in subfloor.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Whether the entity's current position has a "Floor-type" tile above its current position.
        /// </summary>
        public bool IsUnderCover { get; set; } = false;

        /*
         * An un-anchored hiding entity would require listening to on-move events in case it moves into a sub-floor
         * tile. Also T-Ray scanner headaches.
        /// <summary>
        ///     This entity needs to be anchored to be hid when not in subfloor.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; set; } = true;
        */

        public override ComponentState GetComponentState()
        {
            return new SubFloorHideComponentState(Enabled);
        }

        /// <summary>
        ///     Whether or not this entity is supposed
        ///     to be visible.
        /// </summary>
        [ViewVariables]
        public bool Visible { get; set; }

        /// <summary>
        ///     The entities this subfloor is revealed by.
        /// </summary>
        [ViewVariables]
        public HashSet<EntityUid> RevealedBy { get; set; } = new();
    }

    [Serializable, NetSerializable]
    public sealed class SubFloorHideComponentState : ComponentState
    {
        public bool Enabled { get; }

        public SubFloorHideComponentState(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
