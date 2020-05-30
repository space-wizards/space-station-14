using System;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : SharedHotbarComponent
    {
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case HotbarActionMessage msg:
                {
                    HandleHotbarActionMsg(msg);
                    break;
                }
            }
        }

        private void HandleHotbarActionMsg(HotbarActionMessage msg)
        {
            var eventArgs = new HotbarActionEventArgs(Owner, msg.Id, msg.Enabled);

            if (Owner.TryGetComponent<HandsComponent>(out var handsComponent))
            {
                foreach (var itemComponent in handsComponent.GetAllHeldItems())
                {
                    var actionComponents = itemComponent.Owner.GetAllComponents<IHotbarAction>();

                    foreach (var component in actionComponents)
                    {
                        component.HotbarAction(eventArgs);
                    }
                }
            }

            if (Owner.TryGetComponent<InventoryComponent>(out var inventoryComponent))
            {
                foreach (var (slot, container) in inventoryComponent.SlotContainers)
                {
                    if (container.ContainedEntity == null)
                    {
                        continue;
                    }

                    var actionComponents = container.ContainedEntity.GetAllComponents<IHotbarAction>();

                    foreach (var component in actionComponents)
                    {
                        component.HotbarAction(eventArgs);
                    }
                }
            }
        }
    }

    public interface IHotbarAction
    {
        void HotbarAction(HotbarActionEventArgs eventArgs);
    }

    public class HotbarActionEventArgs : EventArgs
    {
        public IEntity User { get; }
        public HotbarActionId AbilityId { get; }
        public bool Enabled { get; }

        public HotbarActionEventArgs(IEntity user, HotbarActionId abilityId, bool enabled)
        {
            User = user;
            AbilityId = abilityId;
            Enabled = enabled;
        }
    }
}
