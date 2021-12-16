using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Clothing;
using Content.Shared.CharacterAppearance;
using Content.Shared.Inventory;
using Content.Shared.Movement.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;
using static Content.Shared.Inventory.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Client.Inventory
{
    /// <summary>
    /// A character UI which shows items the user has equipped within his inventory
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedInventoryComponent))]
    public class ClientInventoryComponent : SharedInventoryComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private readonly Dictionary<Slots, EntityUid> _slots = new();

        public IReadOnlyDictionary<Slots, EntityUid> AllSlots => _slots;

        [ViewVariables] public InventoryInterfaceController InterfaceController { get; private set; } = default!;

        [ComponentDependency]
        private ISpriteComponent? _sprite;

        private bool _playerAttached = false;

        [ViewVariables]
        [DataField("speciesId")] public string? SpeciesId { get; set; }

        protected override void OnRemove()
        {
            base.OnRemove();

            if (_playerAttached)
            {
                InterfaceController?.PlayerDetached();
            }
            InterfaceController?.Dispose();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var controllerType = ReflectionManager.LooseGetType(InventoryInstance.InterfaceControllerTypeName);
            var args = new object[] {this};
            InterfaceController = DynamicTypeFactory.CreateInstance<InventoryInterfaceController>(controllerType, args);
            InterfaceController.Initialize();

            if (_sprite != null)
            {
                foreach (var mask in InventoryInstance.SlotMasks.OrderBy(s => InventoryInstance.SlotDrawingOrder(s)))
                {
                    if (mask == Slots.NONE)
                    {
                        continue;
                    }

                    _sprite.LayerMapReserveBlank(mask);
                }
            }

            // Component state already came in but we couldn't set anything visually because, well, we didn't initialize yet.
            foreach (var (slot, entity) in _slots)
            {
                _setSlot(slot, entity);
            }
        }

        public override bool IsEquipped(EntityUid item)
        {
            return item != default && _slots.Values.Any(e => e == item);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not InventoryComponentState state)
                return;

            var doneSlots = new HashSet<Slots>();

            foreach (var (slot, entity) in state.Entities)
            {
                if (!_entMan.EntityExists(entity))
                {
                    continue;
                }
                if (!_slots.ContainsKey(slot) || _slots[slot] != entity)
                {
                    _slots[slot] = entity;
                    _setSlot(slot, entity);
                }
                doneSlots.Add(slot);
            }

            if (state.HoverEntity != null)
            {
                var (slot, (entity, fits)) = state.HoverEntity.Value;
                InterfaceController?.HoverInSlot(slot, entity, fits);
            }

            foreach (var slot in _slots.Keys.ToList())
            {
                if (!doneSlots.Contains(slot))
                {
                    _clearSlot(slot);
                    _slots.Remove(slot);
                }
            }

            EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(Owner);
        }

        private void _setSlot(Slots slot, EntityUid entity)
        {
            SetSlotVisuals(slot, entity);

            InterfaceController?.AddToSlot(slot, entity);
        }

        internal void SetSlotVisuals(Slots slot, EntityUid entity)
        {
            if (_sprite == null)
            {
                return;
            }

            if (_entMan.TryGetComponent(entity, out ClothingComponent? clothing))
            {
                var flag = SlotMasks[slot];
                var data = clothing.GetEquippedStateInfo(flag, SpeciesId);
                if (data != null)
                {
                    var (rsi, state) = data.Value;
                    _sprite.LayerSetVisible(slot, true);
                    _sprite.LayerSetState(slot, state, rsi);
                    _sprite.LayerSetAutoAnimated(slot, true);

                    if (slot == Slots.INNERCLOTHING && _sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
                    {
                        _sprite.LayerSetState(HumanoidVisualLayers.StencilMask, clothing.FemaleMask switch
                        {
                            FemaleClothingMask.NoMask => "female_none",
                            FemaleClothingMask.UniformTop => "female_top",
                            _ => "female_full",
                        });
                    }

                    return;
                }
            }

            _sprite.LayerSetVisible(slot, false);
        }

        internal void ClearAllSlotVisuals()
        {
            if (_sprite == null)
                return;

            foreach (var slot in InventoryInstance.SlotMasks)
            {
                if (slot != Slots.NONE)
                {
                    _sprite.LayerSetVisible(slot, false);
                }
            }
        }

        private void _clearSlot(Slots slot)
        {
            InterfaceController?.RemoveFromSlot(slot);
            _sprite?.LayerSetVisible(slot, false);
        }

        public void SendEquipMessage(Slots slot)
        {
            var equipMessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Equip);
#pragma warning disable 618
            SendNetworkMessage(equipMessage);
#pragma warning restore 618
        }

        public void SendUseMessage(Slots slot)
        {
            var equipmessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Use);
#pragma warning disable 618
            SendNetworkMessage(equipmessage);
#pragma warning restore 618
        }

        public void SendHoverMessage(Slots slot)
        {
#pragma warning disable 618
            SendNetworkMessage(new ClientInventoryMessage(slot, ClientInventoryUpdate.Hover));
#pragma warning restore 618
        }

        public void SendOpenStorageUIMessage(Slots slot)
        {
#pragma warning disable 618
            SendNetworkMessage(new OpenSlotStorageUIMessage(slot));
#pragma warning restore 618
        }

        public void PlayerDetached()
        {
            InterfaceController.PlayerDetached();
            _playerAttached = false;
        }

        public void PlayerAttached()
        {
            InterfaceController.PlayerAttached();
            _playerAttached = true;
        }

        public override bool TryGetSlot(Slots slot, [NotNullWhen(true)] out EntityUid? item)
        {
            // dict TryGetValue uses default EntityUid, not null.
            if (!_slots.ContainsKey(slot))
            {
                item = null;
                return false;
            }

            item = _slots[slot];
            return item != null;
        }

        public bool TryFindItemSlots(EntityUid item, [NotNullWhen(true)] out Slots? slots)
        {
            slots = null;

            foreach (var (slot, entity) in _slots)
            {
                if (entity == item)
                {
                    slots = slot;
                    return true;
                }
            }

            return false;
        }
    }
}
