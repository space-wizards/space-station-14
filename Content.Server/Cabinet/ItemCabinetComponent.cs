using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cabinet
{
    /// <summary>
    ///     Used for entities that can be opened, closed, and can hold one item. E.g., fire extinguisher cabinets.
    /// </summary>
    [RegisterComponent]
    public class ItemCabinetComponent : Component
    {
        public override string Name => "ItemCabinet";

        /// <summary>
        ///     Sound to be played when the cabinet door is opened.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("doorSound", required: true)]
        public SoundSpecifier DoorSound { get; set; } = default!;

        /// <summary>
        ///     The slot name, used to get the actual item slot from the ItemSlotsComponent.
        /// </summary>
        [DataField("cabinetSlot")]
        public string CabinetSlot = "cabinetSlot";

        /// <summary>
        ///     Whether the cabinet is currently open or not.
        /// </summary>
        [ViewVariables]
        [DataField("opened")]
        public bool Opened { get; set; } = false;
    }
}
