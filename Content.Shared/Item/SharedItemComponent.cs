using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Shared.Item
{
    /// <summary>
    ///    Players can pick up, drop, and put items in bags, and they can be seen in player's hands.
    /// </summary>
    [NetworkedComponent()]
    public abstract class SharedItemComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        ///     How much big this item is.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                Dirty();
            }
        }
        [DataField("size")]
        private int _size;

        [DataField("inhandVisuals")]
        public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

        [DataField("clothingVisuals")]
        public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

        /// <summary>
        ///     Whether or not this item can be picked up.
        /// </summary>
        /// <remarks>
        ///     This should almost always be true for items. But in some special cases, an item can be equipped but not
        ///     picked up. E.g., hardsuit helmets are attached to the suit, so we want to disable things like the pickup
        ///     verb.
        /// </remarks>
        [DataField("canPickup")]
        public bool CanPickup = true;

        [DataField("quickEquip")]
        public bool QuickEquip = true;

        /// <summary>
        ///     Part of the state of the sprite shown on the player when this item is in their hands or inventory.
        /// </summary>
        /// <remarks>
        ///     Only used if <see cref="InhandVisuals"/> or <see cref="ClothingVisuals"/> are unspecified.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                EntitySystem.Get<SharedItemSystem>().VisualsChanged(Owner, this);
                Dirty();
            }
        }
        [DataField("HeldPrefix")]
        private string? _equippedPrefix;

        [ViewVariables]
        [DataField("Slots")]
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        [DataField("equipSound")]
        public SoundSpecifier? EquipSound { get; set; } = default!;

        [DataField("unequipSound")]
        public SoundSpecifier? UnequipSound = default!;
        
        /// <summary>
        ///     Rsi of the sprite shown on the player when this item is in their hands. Used to generate a default entry for <see cref="InhandVisuals"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sprite")]
        public readonly string? RsiPath;

        public void RemovedFromSlot()
        {
            if (_entMan.TryGetComponent(Owner, out SharedSpriteComponent? component))
                component.Visible = true;
        }

        public virtual void EquippedToSlot()
        {
            if (_entMan.TryGetComponent(Owner, out SharedSpriteComponent? component))
                component.Visible = false;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ItemComponentState : ComponentState
    {
        public int Size { get; }
        public string? EquippedPrefix { get; }

        public ItemComponentState(int size, string? equippedPrefix)
        {
            Size = size;
            EquippedPrefix = equippedPrefix;
        }
    }

    /// <summary>
    ///     Raised when an item's visual state is changed. The event is directed at the entity that contains this item, so
    ///     that it can properly update its hands or inventory sprites and GUI.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VisualsChangedEvent : EntityEventArgs
    {
        public readonly EntityUid Item;
        public readonly string ContainerId;

        public VisualsChangedEvent(EntityUid item, string containerId)
        {
            Item = item;
            ContainerId = containerId;
        }
    }

    /// <summary>
    ///     Reference sizes for common containers and items.
    /// </summary>
    public enum ReferenceSizes
    {
        Wallet = 4,
        Pocket = 12,
        Box = 24,
        Belt = 30,
        Toolbox = 60,
        Backpack = 100,
        NoStoring = 9999
    }
}
