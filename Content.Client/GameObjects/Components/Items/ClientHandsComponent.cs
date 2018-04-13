using Content.Client.Interfaces.GameObjects;
using Content.Client.UserInterface;
using Content.Shared.GameObjects;
using SS14.Client.Interfaces.UserInterface;
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
                IEntity entity = null;
                try
                {
                    entity = Owner.EntityManager.GetEntity(hand.Value);
                }
                catch
                {
                    // Nothing.
                }
                hands[hand.Key] = entity;
            }

            ActiveIndex = cast.ActiveIndex;
            // Tell UI to update.
            var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            if (!uiMgr.StateRoot.TryGetChild<HandsGui>("HandsGui", out var control))
            {
                control = new HandsGui();
                uiMgr.StateRoot.AddChild(control);
            }
            control.UpdateHandIcons();
        }

        public void SendChangeHand(string index)
        {
            SendNetworkMessage(new ClientChangedHandMsg(index));
        }

        public void UseActiveHand()
        {
            SendNetworkMessage(new ActivateInhandMsg());
        }
    }
}
