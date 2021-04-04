using Content.Server.GameObjects.Components.Medical;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Medical
{
    public class CloneMindReturnSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloneMindReturnComponent, MindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloneMindReturnComponent, MindRemovedMessage>(HandleMindRemoved);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<CloneMindReturnComponent, MindAddedMessage>(HandleMindAdded);
            UnsubscribeLocalEvent<CloneMindReturnComponent, MindRemovedMessage>(HandleMindRemoved);
        }

        private void HandleMindAdded(EntityUid uid, CloneMindReturnComponent component, MindAddedMessage args)
        {
            if (component.Parent == EntityUid.Invalid)
                return;

            RaiseLocalEvent(component.Parent, new CloneMindAddedMessage(uid));
        }

        private void HandleMindRemoved(EntityUid uid, CloneMindReturnComponent component, MindRemovedMessage args)
        {
            if (component.Parent == EntityUid.Invalid)
                return;

            RaiseLocalEvent(component.Parent, new CloneMindRemovedMessage(uid));
        }
    }

    public class CloneMindRemovedMessage : EntityEventArgs
    {
        public EntityUid Uid;

        public CloneMindRemovedMessage(EntityUid uid)
        {
            Uid = uid;
        }
    }

    public class CloneMindAddedMessage : EntityEventArgs
    {
        public EntityUid Uid;

        public CloneMindAddedMessage(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
