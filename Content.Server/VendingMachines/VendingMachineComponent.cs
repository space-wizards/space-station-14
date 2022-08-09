using Content.Shared.Actions.ActionTypes;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedVendingMachineComponent))]
    [Access(typeof(VendingMachineSystem))]
    public sealed class VendingMachineComponent : SharedVendingMachineComponent
    {
        public bool Ejecting;
        public bool Denying;

        public string? NextItemToEject;

        public bool Broken;

        /// <summary>
        /// When true, will forcefully throw any object it dispenses
        /// </summary>
        [DataField("speedLimiter")]
        public bool CanShoot = false;

        public bool ThrowNextItem = false;

        /// <summary>
        ///     The chance that a vending machine will randomly dispense an item on hit.
        ///     Chance is 0 if null.
        /// </summary>
        [DataField("dispenseOnHitChance")]
        public float? DispenseOnHitChance;

        /// <summary>
        ///     The minimum amount of damage that must be done per hit to have a chance
        ///     of dispensing an item.
        /// </summary>
        [DataField("dispenseOnHitThreshold")]
        public float? DispenseOnHitThreshold;

        /// <summary>
        ///     Sound that plays when ejecting an item
        /// </summary>
        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");

        /// <summary>
        ///     Sound that plays when an item can't be ejected
        /// </summary>
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        /// <summary>
        ///     The action available to the player controlling the vending machine
        /// </summary>
        [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string? Action = "VendingThrow";

        public float NonLimitedEjectForce = 7.5f;

        public float NonLimitedEjectRange = 5f;

        public float EjectAccumulator = 0f;
        public float DenyAccumulator = 0f;
    }
}
