#nullable enable
using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Storage;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [ComponentReference(typeof(ItemComponent))]
    public class ClothingComponent : ItemComponent
    {
        [DataField("femaleMask")]
        private FemaleClothingMask _femaleMask = FemaleClothingMask.UniformFull;
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;

        private string? _clothingEquippedPrefix;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ClothingPrefix")]
        public string? ClothingEquippedPrefix
        {
            get => _clothingEquippedPrefix;
            set
            {
                if (_clothingEquippedPrefix == value)
                    return;

                _clothingEquippedPrefix = value;

                if(!Initialized) return;

                if (!Owner.TryGetContainer(out IContainer? container))
                    return;
                if (!container.Owner.TryGetComponent(out ClientInventoryComponent? inventory))
                    return;
                if (!inventory.TryFindItemSlots(Owner, out EquipmentSlotDefines.Slots? slots))
                    return;

                inventory.SetSlotVisuals(slots.Value, Owner);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            ClothingEquippedPrefix = ClothingEquippedPrefix;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public FemaleClothingMask FemaleMask
        {
            get => _femaleMask;
            set => _femaleMask = value;
        }

        public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EquipmentSlotDefines.SlotFlags slot, string? speciesId=null)
        {
            if (RsiPath == null)
                return null;

            var rsi = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / RsiPath).RSI;
            var prefix = ClothingEquippedPrefix ?? EquippedPrefix;
            var stateId = prefix != null ? $"{prefix}-equipped-{slot}" : $"equipped-{slot}";
            if (speciesId != null)
            {
                var speciesState = $"{stateId}-{speciesId}";
                if (rsi.TryGetState(speciesState, out _))
                {
                    return (rsi, speciesState);
                }
            }

            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId);
            }

            return null;
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not ClothingComponentState state)
            {
                return;
            }

            ClothingEquippedPrefix = state.ClothingEquippedPrefix;
            EquippedPrefix = state.EquippedPrefix;
        }
    }

    public enum FemaleClothingMask : byte
    {
        NoMask = 0,
        UniformFull,
        UniformTop
    }
}
