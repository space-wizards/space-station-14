using Content.Server.Body.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in internals as breath tool.
    /// </summary>
    [RegisterComponent]
    public class BreathToolComponent : Component, IEquipped, IUnequipped
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        /// <summary>
        /// Tool is functional only in allowed slots
        /// </summary>
        [DataField("allowedSlots")]
        private EquipmentSlotDefines.SlotFlags _allowedSlots = EquipmentSlotDefines.SlotFlags.MASK;

        public override string Name => "BreathMask";
        public bool IsFunctional { get; private set; }
        public EntityUid ConnectedInternalsEntity { get; private set; }

        protected override void Shutdown()
        {
            base.Shutdown();
            DisconnectInternals();
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            if ((EquipmentSlotDefines.SlotMasks[eventArgs.Slot] & _allowedSlots) != _allowedSlots) return;
            IsFunctional = true;

            if (_entities.TryGetComponent(eventArgs.User, out InternalsComponent? internals))
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
            ConnectedInternalsEntity = default;

            if (old != default && _entities.TryGetComponent<InternalsComponent?>(old, out var internalsComponent))
            {
                internalsComponent.DisconnectBreathTool();
            }

            IsFunctional = false;
        }
    }
}
