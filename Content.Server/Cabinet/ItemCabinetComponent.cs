using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cabinet
{
    /// <summary>
    ///     Used for entities that can hold one item that fits the whitelist, which can be extracted by interacting with
    ///     the entity, and can have an item fitting the whitelist placed back inside
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
        ///     The prototype that should be spawned inside the cabinet when it is map initialized.
        /// </summary>
        [ViewVariables]
        [DataField("spawnPrototype")]
        public string? SpawnPrototype { get; set; }

        /// <summary>
        ///     A whitelist defining which entities are allowed into the cabinet.
        /// </summary>
        [ViewVariables]
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist = null;

        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

        /// <summary>
        ///     Whether the cabinet is currently open or not.
        /// </summary>
        [ViewVariables]
        [DataField("opened")]
        public bool Opened { get; set; } = false;
    }
}
