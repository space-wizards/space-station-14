using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class SecretStashSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecretStashComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SecretStashComponent, DestructionEventArgs>(OnDestroyed);
        }

        private void OnInit(EntityUid uid, SecretStashComponent component, ComponentInit args)
        {
            component.ItemContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "stash", out _);
        }

        private void OnDestroyed(EntityUid uid, SecretStashComponent component, DestructionEventArgs args)
        {
            _containerSystem.EmptyContainer(component.ItemContainer);
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
            HandsComponent? hands = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (!Resolve(itemToHideUid, ref item))
                return false;
            if (!Resolve(userUid, ref hands))
                return false;

            // check if secret stash is already occupied
            var container = component.ItemContainer;
            if (container.ContainedEntity != null)
            {
                var msg = Loc.GetString("comp-secret-stash-action-hide-container-not-empty");
                _popupSystem.PopupEntity(msg, uid, userUid);
                return false;
            }

            // check if item is too big to fit into secret stash
            if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(component.MaxItemSize))
            {
                var msg = Loc.GetString("comp-secret-stash-action-hide-item-too-big",
                    ("item", itemToHideUid), ("stash", GetSecretPartName(uid, component)));
                _popupSystem.PopupEntity(msg, uid, userUid);
                return false;
            }

            // try to move item from hands to stash container
            if (!_handsSystem.TryDropIntoContainer(userUid, itemToHideUid, container))
            {
                return false;
            }

            // all done, show success message
            var successMsg = Loc.GetString("comp-secret-stash-action-hide-success",
                ("item", itemToHideUid), ("this", GetSecretPartName(uid, component)));
            _popupSystem.PopupEntity(successMsg, uid, userUid);
            return true;
        }

        /// <summary>
        ///     Try get item and place it in users hand.
        ///     If user can't take it by hands, will drop item from container.
        /// </summary>
        /// <returns>True if user received item</returns>
        public bool TryGetItem(EntityUid uid, EntityUid userUid, SecretStashComponent? component = null,
            HandsComponent? hands = null)
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

            _handsSystem.PickupOrDrop(userUid, container.ContainedEntity.Value, handsComp: hands);

            // show success message
            var successMsg = Loc.GetString("comp-secret-stash-action-get-item-found-something",
                ("stash", GetSecretPartName(uid, component)));
            _popupSystem.PopupEntity(successMsg, uid, userUid);

            return true;
        }

        private string GetSecretPartName(EntityUid uid, SecretStashComponent stash)
        {
            if (stash.SecretPartName != "")
                return Loc.GetString(stash.SecretPartName);

            var entityName = Loc.GetString("comp-secret-stash-secret-part-name", ("this", uid));

            return entityName;
        }
    }
}
