using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using static Content.Shared.GameObjects.EntitySystemMessages.HandsSystemMessages;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ChangeHandMessage>(OnChangeHand);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeNetworkEvent<ChangeHandMessage>();
        }

        private void OnChangeHand(ChangeHandMessage msg, EntitySessionEventArgs args)
        {
            var entity = args.SenderSession.AttachedEntity;

            if (entity == null ||
                !entity.TryGetComponent(out SharedHandsComponent hands))
            {
                return;
            }

            hands.ActiveHand = msg.Index;
        }
    }
}
