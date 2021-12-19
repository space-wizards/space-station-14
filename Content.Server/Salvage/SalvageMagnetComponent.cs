using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Salvage
{
    /// <summary>
    ///     A salvage magnet.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("SalvageMagnet")]
    public class SalvageMagnetComponent : Component
    {
        /// <summary>
        ///     Offset relative to magnet that salvage should spawn.
        ///     Keep in sync with marker sprite (if any???)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offset")]
        public Vector2 Offset = Vector2.Zero; // TODO: Maybe specify a direction, and find the nearest edge of the magnets grid the salvage can fit at


        /// <summary>
        ///     The entity attached to the magnet
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("attachedEntity")]
        public EntityUid? AttachedEntity = null;

        /// <summary>
        ///     Current state of this magnet
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("magnetState")]
        public MagnetState MagnetState = MagnetState.Inactive;
    }
    public record struct MagnetState(MagnetStateType StateType, TimeSpan Until)
    {
        public static readonly MagnetState Inactive = new (MagnetStateType.Inactive, TimeSpan.Zero);
    };
    public enum MagnetStateType
    {
        Inactive,
        Attaching,
        Holding,
        Detaching,
        CoolingDown,
    }
}
