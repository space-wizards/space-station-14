using Content.Server.Clothing.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Acts;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems
{
    public class WrappedStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WrappedStorageComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WrappedStorageComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInit(EntityUid uid, WrappedStorageComponent component, ComponentInit args)
        {
            component.ItemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(uid, "wrap", out _);
        }

        private void OnUseInHand(EntityUid uid, WrappedStorageComponent component, UseInHandEvent args)
        {
            if (component.ItemContainer.ContainedEntity != null)
            {
                var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                Comp<SharedHandsComponent>(args.User).PutInHandOrDrop(ent);
            }
            QueueDel(uid);
        }
    }
}
