using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
    public sealed class SubFloorHideComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "SubFloorHide";

        /// <summary>
        ///     Whether the entity will be hid when not in subfloor.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     This entity needs to be anchored to be hid when not in subfloor.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; set; } = true;

        public override ComponentState GetComponentState()
        {
            return new SubFloorHideComponentState(Enabled, RequireAnchored);
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

        /// <summary>
        ///     Whether or not this entity was revealed with or without
        ///     an entity.
        /// </summary>
        [ViewVariables]
        public bool RevealedWithoutEntity { get; set; }
    }

    [Serializable, NetSerializable]
    public class SubFloorHideComponentState : ComponentState
    {
        public bool Enabled { get; }
        public bool RequireAnchored { get; }

        public SubFloorHideComponentState(bool enabled, bool requireAnchored)
        {
            Enabled = enabled;
            RequireAnchored = requireAnchored;
        }
    }
}
