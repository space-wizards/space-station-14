using System;
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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SubFloorHideComponentState(Enabled, RequireAnchored);
        }
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
