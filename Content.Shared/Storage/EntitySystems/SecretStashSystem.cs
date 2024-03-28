using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Examine;

namespace Content.Shared.Storage.EntitySystems
{
    /// <summary>
    /// Secret Stash allows an item to be hidden within.
    /// </summary>
    public sealed class SecretStashSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedToolSystem _tool = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecretStashComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SecretStashComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<SecretStashComponent, StashPryDoAfterEvent>(OnSecretStashPried);
            SubscribeLocalEvent<SecretStashComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SecretStashComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SecretStashComponent, ExaminedEvent>(OnExamine);
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

        private void OnInteractUsing(EntityUid uid, SecretStashComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!component.OpenableStash)
                return;

            // is player trying place or lift off cistern lid?
            if (_tool.UseTool(args.Used, args.User, uid, component.PryDoorTime, component.PryingQuality, new StashPryDoAfterEvent()))
                args.Handled = true;
            // maybe player is trying to hide something inside cistern?
            else if (component.ToggleOpen)
            {
                TryHideItem(uid, args.User, args.Used);
                args.Handled = true;
            }
        }

        private void OnInteractHand(EntityUid uid, SecretStashComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (!component.OpenableStash)
                return;

            // trying to get something from stash?
            if (component.ToggleOpen)
            {
                var gotItem = TryGetItem(uid, args.User);
                if (gotItem)
                {
                    args.Handled = true;
                    return;
                }
            }
            args.Handled = true;
        }

        private void OnSecretStashPried(EntityUid uid, SecretStashComponent component, StashPryDoAfterEvent args)
        {
            if (args.Cancelled)
                return;

            ToggleOpen(uid, component);
        }

        public void ToggleOpen(EntityUid uid, SecretStashComponent? component = null, MetaDataComponent? meta = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.ToggleOpen = !component.ToggleOpen;

            UpdateAppearance(uid, component);
            Dirty(uid, component, meta);
        }

        private void UpdateAppearance(EntityUid uid, SecretStashComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _appearance.SetData(uid, StashVisuals.DoorVisualState, component.ToggleOpen ? DoorVisualState.DoorOpen : DoorVisualState.DoorClosed);
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
                _popupSystem.PopupClient(msg, uid, userUid);
                return false;
            }

            // check if item is too big to fit into secret stash
            if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(component.MaxItemSize))
            {
                var msg = Loc.GetString("comp-secret-stash-action-hide-item-too-big",
                    ("item", itemToHideUid), ("stash", GetSecretPartName(uid, component)));
                _popupSystem.PopupClient(msg, uid, userUid);
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
            _popupSystem.PopupClient(successMsg, uid, userUid);
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
            _popupSystem.PopupClient(successMsg, uid, userUid);

            return true;
        }

        private void OnExamine(EntityUid uid, SecretStashComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange && component.ToggleOpen)
            {
                if (HasItemInside(uid))
                {
                    var msg = Loc.GetString(component.ExamineStash);
                    args.PushMarkup(msg);
                }
            }
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
