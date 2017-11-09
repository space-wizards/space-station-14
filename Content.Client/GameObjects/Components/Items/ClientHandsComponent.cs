using Content.Client.Interfaces.GameObjects;
using Content.Client.UserInterface;
using Content.Shared.GameObjects;
using Lidgren.Network;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace Content.Client.GameObjects
{
    public class HandsComponent : SharedHandsComponent, IHandsComponent
    {
        private readonly Dictionary<string, IEntity> hands = new Dictionary<string, IEntity>();
        public string ActiveIndex { get; private set; }

        public IEntity GetEntity(string index)
        {
            if (hands.TryGetValue(index, out var entity))
            {
                return entity;
            }

            return null;
        }

        public override void HandleComponentState(ComponentState state)
        {
            var cast = (HandsComponentState)state;
            hands.Clear();
            foreach (var hand in cast.Hands)
            {
                hands[hand.Key] = Owner.EntityManager.GetEntity(hand.Value);
            }

            ActiveIndex = cast.ActiveIndex;

            // Tell UI to update.
            var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            if (!uiMgr.TryGetSingleComponent<HandsGui>(out var component))
            {
                component = new HandsGui();
                uiMgr.AddComponent(component);
            }
            component.UpdateHandIcons();
        }

        public void SendChangeHand(string index)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, index);
        }
    }
}
