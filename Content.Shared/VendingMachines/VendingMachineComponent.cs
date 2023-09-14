using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.VendingMachines
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class VendingMachineComponent : Component
    {
        /// <summary>
        /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
        /// </summary>
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>), required: true)]
        public string PackPrototypeId = string.Empty;

        /// <summary>
        /// Used by the server to determine how long the vending machine stays in the "Deny" state.
        /// Used by the client to determine how long the deny animation should be played.
        /// </summary>
        [DataField("denyDelay")]
        public float DenyDelay = 2.0f;

        /// <summary>
        /// Used by the server to determine how long the vending machine stays in the "Eject" state.
        /// The selected item is dispensed afer this delay.
        /// Used by the client to determine how long the deny animation should be played.
        /// </summary>
        [DataField("ejectDelay")]
        public float EjectDelay = 1.2f;

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

        public bool Contraband;

        public bool Ejecting;
        public bool Denying;
        public bool DispenseOnHitCoolingDown;

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
        ///     Amount of time in seconds that need to pass before damage can cause a vending machine to eject again.
        ///     This value is separate to <see cref="VendingMachineComponent.EjectDelay"/> because that value might be
        ///     0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
        ///     and can be circumvented with forced ejections.
        /// </summary>
        [DataField("dispenseOnHitCooldown")]
        public float? DispenseOnHitCooldown = 1.0f;

        /// <summary>
        ///     Sound that plays when ejecting an item
        /// </summary>
        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
        {
            Params = new AudioParams
            {
                Volume = -2f
            }
        };

        /// <summary>
        ///     Sound that plays when an item can't be ejected
        /// </summary>
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        /// <summary>
        ///     The action available to the player controlling the vending machine
        /// </summary>
        [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        [AutoNetworkedField]
        public string? Action = "ActionVendingThrow";

        [DataField("actionEntity")]
        [AutoNetworkedField]
        public EntityUid? ActionEntity;

        public float NonLimitedEjectForce = 7.5f;

        public float NonLimitedEjectRange = 5f;

        public float EjectAccumulator = 0f;
        public float DenyAccumulator = 0f;
        public float DispenseOnHitAccumulator = 0f;

        /// <summary>
        ///     While disabled by EMP it randomly ejects items
        /// </summary>
        [DataField("nextEmpEject", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextEmpEject = TimeSpan.Zero;

        #region Client Visuals
        /// <summary>
        /// RSI state for when the vending machine is unpowered.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
        /// </summary>
        [DataField("offState")]
        public string? OffState;

        /// <summary>
        /// RSI state for the screen of the vending machine
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Screen"/>
        /// </summary>
        [DataField("screenState")]
        public string? ScreenState;

        /// <summary>
        /// RSI state for the vending machine's normal state. Usually a looping animation.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
        /// </summary>
        [DataField("normalState")]
        public string? NormalState;

        /// <summary>
        /// RSI state for the vending machine's eject animation.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
        /// </summary>
        [DataField("ejectState")]
        public string? EjectState;

        /// <summary>
        /// RSI state for the vending machine's deny animation. Will either be played once as sprite flick
        /// or looped depending on how <see cref="LoopDenyAnimation"/> is set.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
        /// </summary>
        [DataField("denyState")]
        public string? DenyState;

        /// <summary>
        /// RSI state for when the vending machine is unpowered.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
        /// </summary>
        [DataField("brokenState")]
        public string? BrokenState;

        /// <summary>
        /// If set to <c>true</c> (default) will loop the animation of the <see cref="DenyState"/> for the duration
        /// of <see cref="VendingMachineComponent.DenyDelay"/>. If set to <c>false</c> will play a sprite
        /// flick animation for the state and then linger on the final frame until the end of the delay.
        /// </summary>
        [DataField("loopDeny")]
        public bool LoopDenyAnimation = true;
        #endregion
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineInventoryEntry
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public InventoryType Type;
        [ViewVariables(VVAccess.ReadWrite)]
        public string ID;
        [ViewVariables(VVAccess.ReadWrite)]
        public uint Amount;
        public VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
        {
            Type = type;
            ID = id;
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public enum InventoryType : byte
    {
        Regular,
        Emagged,
        Contraband
    }

    [Serializable, NetSerializable]
    public enum VendingMachineVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum VendingMachineVisualState
    {
        Normal,
        Off,
        Broken,
        Eject,
        Deny,
    }

    public enum VendingMachineVisualLayers : byte
    {
        /// <summary>
        /// Off / Broken. The other layers will overlay this if the machine is on.
        /// </summary>
        Base,
        /// <summary>
        /// Normal / Deny / Eject
        /// </summary>
        BaseUnshaded,
        /// <summary>
        /// Screens that are persistent (where the machine is not off or broken)
        /// </summary>
        Screen
    }

    [Serializable, NetSerializable]
    public enum ContrabandWireKey : byte
    {
        StatusKey,
        TimeoutKey
    }

    [Serializable, NetSerializable]
    public enum EjectWireKey : byte
    {
        StatusKey,
    }

    public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent
    {

    };
}
