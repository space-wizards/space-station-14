using Content.Shared.Containers.ItemSlots;
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
        ///     The <see cref="ItemSlot"/> that stores the actual item. The entity whitelist, sounds, and other
        ///     behaviours are specified by this <see cref="ItemSlot"/> definition.
        /// </summary>
        [DataField("cabinetSlot")]
        public ItemSlot CabinetSlot = new();

        /// <summary>
        ///     Whether the cabinet is currently open or not.
        /// </summary>
        [ViewVariables]
        [DataField("opened")]
        public bool Opened { get; set; } = false;
    }
}
