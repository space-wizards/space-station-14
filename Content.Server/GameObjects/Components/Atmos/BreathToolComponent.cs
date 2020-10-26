#nullable enable
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Used in internals as breath tool.
    /// </summary>
    [RegisterComponent]
    public class BreathToolComponent : Component, IEquipped, IUnequipped
    {
        /// <summary>
        /// Tool is functional only in allowed slots
        /// </summary>
        private EquipmentSlotDefines.SlotFlags _allowedSlots;

        public override string Name => "BreathMask";
        public bool IsFunctional { get; private set; }
        public IEntity? ConnectedInternalsEntity { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _allowedSlots, "allowedSlots", EquipmentSlotDefines.SlotFlags.MASK);
        }

        public void Equipped(EquippedEventArgs eventArgs)
        {
            if ((EquipmentSlotDefines.SlotMasks[eventArgs.Slot] & _allowedSlots) != _allowedSlots) return;
            IsFunctional = true;
        }

        public void Unequipped(UnequippedEventArgs eventArgs)
        {
            IsFunctional = false;
            if (ConnectedInternalsEntity != null)
            {
                if (ConnectedInternalsEntity.TryGetComponent<InternalsComponent>(out var internalsComponent))
                {
                    internalsComponent.DisconnectBreathTool();
                }

                ConnectedInternalsEntity = null;
            }
        }

        public void DisconnectInternals()
        {
        }
    }
}
