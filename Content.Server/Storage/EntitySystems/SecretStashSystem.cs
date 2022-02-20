using Content.Server.Clothing.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Acts;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class SecretStashSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecretStashComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SecretStashComponent, DestructionEventArgs>(OnDestroyed);
        }

        private void OnInit(EntityUid uid, SecretStashComponent component, ComponentInit args)
        {
            // set default secret part name
            if (component.SecretPartName == "")
            {
                var meta = EntityManager.GetComponent<MetaDataComponent>(uid);
                var entityName = Loc.GetString("comp-secret-stash-secret-part-name", ("name", meta.EntityName));
                component.SecretPartName = entityName;
            }

            component.ItemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(uid, "stash", out _);
        }

        private void OnDestroyed(EntityUid uid, SecretStashComponent component, DestructionEventArgs args)
        {
            component.ItemContainer.EmptyContainer();
        }

        /// <summary>
        ///     Is there something inside secret stash item container?
        /// </summary>
        public bool HasItemInside(EntityUid uid, SecretStashComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            return component.ItemContainer.ContainedEntity != null;
        }

        /// <summary>
        ///     Tries to hide item inside secret stash from hands of user.
        /// </summary>
        /// <returns>True if item was hidden inside stash</returns>
        public bool TryHideItem(EntityUid uid, EntityUid userUid, EntityUid itemToHideUid,
            SecretStashComponent? component = null, ItemComponent? item = null,
            MetaDataComponent? itemMeta = null, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (!Resolve(itemToHideUid, ref item, ref itemMeta))
                return false;
            if (!Resolve(userUid, ref hands))
                return false;

            // check if secret stash is already occupied
            var container = component.ItemContainer;
            if (container.ContainedEntity != null)
            {
                var msg = Loc.GetString("comp-secret-stash-action-hide-container-not-empty");
                _popupSystem.PopupEntity(msg, uid, Filter.Entities(userUid));
                return false;
            }

            // check if item is too big to fit into secret stash
            var itemName = itemMeta.EntityName;
            if (item.Size > component.MaxItemSize)
            {
                var msg = Loc.GetString("comp-secret-stash-action-hide-item-too-big",
                    ("item", itemName), ("stash", component.SecretPartName));
                _popupSystem.PopupEntity(msg, uid, Filter.Entities(userUid));
                return false;
            }

            // try to move item from hands to stash container
            if (!hands.Drop(itemToHideUid, container))
            {
                return false;
            }

            // all done, show success message
            var successMsg = Loc.GetString("comp-secret-stash-action-hide-success",
                ("item", itemName), ("this", component.SecretPartName));
            _popupSystem.PopupEntity(successMsg, uid, Filter.Entities(userUid));
            return true;
        }

        /// <summary>
        ///     Try get item and place it in users hand.
        ///     If user can't take it by hands, will drop item from container.
        /// </summary>
        /// <returns>True if user received item</returns>
        public bool TryGetItem(EntityUid uid, EntityUid userUid, SecretStashComponent? component = null,
            SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (!Resolve(userUid, ref hands))
                return false;

            // check if secret stash has something inside
            var container = component.ItemContainer;
            if (container.ContainedEntity == null)
            {
                return false;
            }

            // get item inside container
            var itemUid = container.ContainedEntity;
            if (!EntityManager.TryGetComponent(itemUid, out ItemComponent? item))
            {
                return false;
            }
            hands.PutInHandOrDrop(item);

            // show success message
            var successMsg = Loc.GetString("comp-secret-stash-action-get-item-found-something",
                ("stash", component.SecretPartName));
            _popupSystem.PopupEntity(successMsg, uid, Filter.Entities(userUid));

            return true;
        }
    }
}
