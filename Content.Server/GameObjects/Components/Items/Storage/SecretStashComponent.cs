#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    /// <summary>
    /// Logic for secret single slot stash, like plant pot or toilet cistern
    /// </summary>
    [RegisterComponent]
    public class SecretStashComponent : Component, IDestroyAct
    {
        public override string Name => "SecretStash";

        [ViewVariables] private int _maxItemSize;
        [ViewVariables] private string? _secretPartName;

        [ViewVariables] private ContainerSlot _itemContainer = default!;

        public string SecretPartName => _secretPartName ?? Loc.GetString("{0:theName}", Owner);

        public override void Initialize()
        {
            base.Initialize();
            _itemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "stash", out _);
        }
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _maxItemSize, "maxItemSize", (int) ReferenceSizes.Pocket);
            serializer.DataField(ref _secretPartName, "secretPartName", null);
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
                Owner.PopupMessage(user, Loc.GetString("There's already something in here?!"));
                return false;
            }

            if (!itemToHide.TryGetComponent(out ItemComponent? item))
                return false;

            if (item.Size > _maxItemSize)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("{0:TheName} is too big to fit in {1}!", itemToHide, SecretPartName));
                return false;
            }

            if (!user.TryGetComponent(out IHandsComponent? hands))
                return false;

            if (!hands.Drop(itemToHide, _itemContainer))
                return false;

            Owner.PopupMessage(user, Loc.GetString("You hide {0:theName} in {1}.", itemToHide, SecretPartName));
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

            Owner.PopupMessage(user, Loc.GetString("There was something inside {0}!", SecretPartName));

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
