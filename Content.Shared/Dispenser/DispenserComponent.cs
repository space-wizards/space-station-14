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

namespace Content.Shared.Dispenser
{
    /// <summary>
    /// Used for entities that can simply dispense a single type of item on interact.
    /// Think grab-and-go like a glove from a glove pack or a cup from a water cooler.
    /// </summary>
    [RegisterComponent]
    public sealed class DispenserComponent : Component
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
        /// The allowed entities to restock with.
        /// Will not allow restocking without this.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist? WhiteList;

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
