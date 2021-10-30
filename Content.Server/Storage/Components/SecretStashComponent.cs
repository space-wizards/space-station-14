using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Acts;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    /// <summary>
    /// Logic for secret single slot stash, like plant pot or toilet cistern
    /// </summary>
    [RegisterComponent]
    public class SecretStashComponent : Component, IDestroyAct
    {
        public override string Name => "SecretStash";

        [ViewVariables] [DataField("maxItemSize")]
        private int _maxItemSize = (int) ReferenceSizes.Pocket;

        [ViewVariables] [DataField("secretPartName")]
        private readonly string? _secretPartNameOverride = null;

        [ViewVariables] private ContainerSlot _itemContainer = default!;

        public string SecretPartName => _secretPartNameOverride ?? Loc.GetString("comp-secret-stash-secret-part-name", ("name", Owner.Name));

        protected override void Initialize()
        {
            base.Initialize();
            _itemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "stash", out _);
        }

        /// <summary>
        /// Tries to hide item inside secret stash from hands of user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="itemToHide"></param>
        /// <returns>True if item was hidden inside stash</returns>
        public bool TryHideItem(IEntity user, IEntity itemToHide)
        {
            if (_itemContainer.ContainedEntity != null)
            {
                Owner.PopupMessage(user, Loc.GetString("comp-secret-stash-action-hide-container-not-empty"));
                return false;
            }

            if (!itemToHide.TryGetComponent(out ItemComponent? item))
                return false;

            if (item.Size > _maxItemSize)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("comp-secret-stash-action-hide-item-too-big",("item", itemToHide),("stash", SecretPartName)));
                return false;
            }

            if (!user.TryGetComponent(out IHandsComponent? hands))
                return false;

            if (!hands.Drop(itemToHide, _itemContainer))
                return false;

            Owner.PopupMessage(user, Loc.GetString("comp-secret-stash-action-hide-success", ("item", itemToHide), ("this", SecretPartName)));
            return true;
        }

        /// <summary>
        /// Try get item and place it in users hand
        /// If user can't take it by hands, will drop item from container
        /// </summary>
        /// <param name="user"></param>
        /// <returns>True if user recieved item</returns>
        public bool TryGetItem(IEntity user)
        {
            if (_itemContainer.ContainedEntity == null)
                return false;

            Owner.PopupMessage(user, Loc.GetString("comp-secret-stash-action-get-item-found-something", ("stash", SecretPartName)));

            if (user.TryGetComponent(out HandsComponent? hands))
            {
                if (!_itemContainer.ContainedEntity.TryGetComponent(out ItemComponent? item))
                    return false;
                hands.PutInHandOrDrop(item);
            }
            else if (_itemContainer.Remove(_itemContainer.ContainedEntity))
            {
                _itemContainer.ContainedEntity.Transform.Coordinates = Owner.Transform.Coordinates;
            }

            return true;
        }

        /// <summary>
        /// Is there something inside secret stash item container?
        /// </summary>
        /// <returns></returns>
        public bool HasItemInside()
        {
            return _itemContainer.ContainedEntity != null;
        }

        public void OnDestroy(DestructionEventArgs eventArgs)
        {
            // drop item inside
            if (_itemContainer.ContainedEntity != null)
            {
                _itemContainer.ContainedEntity.Transform.Coordinates = Owner.Transform.Coordinates;
            }
        }
    }
}
