using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Sound;
using Content.Shared.Whitelist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.ItemDispenser
{
    /// <summary>
    /// Used for entities that can simply dispense a single type of item on interact.
    /// Think grab-and-go like a glove from a glove pack or a cup from a water cooler.
    /// </summary>
    [RegisterComponent]
    public sealed class ItemDispenserComponent : Component
    {
        /// <summary>
        /// The entity to initially dispense
        /// </summary>
        [DataField("item", readOnly: true, required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemId = default!;

        /// <summary>
        /// The maximum capacity of the dispenser
        /// </summary>
        [DataField("capacity", required: true)]
        public int Capacity;

        /// <summary>
        /// Whether to fill the dispenser on map init
        /// </summary>
        public bool FillOnInit = true;

        /// <summary>
        /// If the remaining stock can be seen OnExmaine
        /// </summary>
        public bool AllowStockExamine = true;

        /// <summary>
        /// The allowed entities to restock with.
        /// Will not allow restocking without this (to avoid putting random things in the dispenser)
        /// </summary>
        [DataField("restockWhitelist")]
        public EntityWhitelist? RestockWhitelist;

        [DataField("dispenseSound")]
        public SoundSpecifier DispenseSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

        [DataField("restockSound")]
        public SoundSpecifier RestockSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/selector.ogg");

        /// <summary>
        ///     Options used for playing the dispense/restock sounds.
        /// </summary>
        [DataField("soundOptions")]
        public AudioParams SoundOptions = AudioParams.Default;

        public Container? Storage;
    }
}
