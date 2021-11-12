using Content.Server.Body.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Atmos.Components
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
        [DataField("allowedSlots")]
        private EquipmentSlotDefines.SlotFlags _allowedSlots = EquipmentSlotDefines.SlotFlags.MASK;

        public override string Name => "BreathMask";
        public bool IsFunctional { get; private set; }
        public IEntity? ConnectedInternalsEntity { get; private set; }

        protected override void Shutdown()
        {
            base.Shutdown();
            DisconnectInternals();
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            if ((EquipmentSlotDefines.SlotMasks[eventArgs.Slot] & _allowedSlots) != _allowedSlots) return;
            IsFunctional = true;

            if (eventArgs.User.TryGetComponent(out InternalsComponent? internals))
            {
                ConnectedInternalsEntity = eventArgs.User;
                internals.ConnectBreathTool(Owner);
            }
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            DisconnectInternals();
        }

        public void DisconnectInternals()
        {
            var old = ConnectedInternalsEntity;
            ConnectedInternalsEntity = null;

            if (old != null && old.TryGetComponent<InternalsComponent>(out var internalsComponent))
            {
                internalsComponent.DisconnectBreathTool();
            }

            IsFunctional = false;
        }
    }
}
