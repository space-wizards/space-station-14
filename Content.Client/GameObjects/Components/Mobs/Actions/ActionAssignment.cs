using System;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.Actions
{
    public struct ActionAssignment : IEquatable<ActionAssignment>
    {
        private readonly ActionType _actionType;
        private readonly ItemActionType _itemActionType;
        private readonly EntityUid _item;
        public Assignment Assignment { get; private init; }

        private ActionAssignment(Assignment assignment, ActionType actionType, ItemActionType itemActionType, EntityUid item)
        {
            Assignment = assignment;
            _actionType = actionType;
            _itemActionType = itemActionType;
            _item = item;
        }

        /// <param name="actionType">the action type, if our Assignment is Assignment.Action</param>
        /// <returns>true only if our Assignment is Assignment.Action</returns>
        public bool TryGetAction(out ActionType actionType)
        {
            actionType = _actionType;
            return Assignment == Assignment.Action;
        }

        /// <param name="itemActionType">the item action type, if our Assignment is Assignment.ItemActionWithoutItem</param>
        /// <returns>true only if our Assignment is Assignment.ItemActionWithoutItem</returns>
        public bool TryGetItemActionWithoutItem(out ItemActionType itemActionType)
        {
            itemActionType = _itemActionType;
            return Assignment == Assignment.ItemActionWithoutItem;
        }

        /// <param name="itemActionType">the item action type, if our Assignment is Assignment.ItemActionWithItem</param>
        /// <param name="item">the item UID providing the action, if our Assignment is Assignment.ItemActionWithItem</param>
        /// <returns>true only if our Assignment is Assignment.ItemActionWithItem</returns>
        public bool TryGetItemActionWithItem(out ItemActionType itemActionType, out EntityUid item)
        {
            itemActionType = _itemActionType;
            item = _item;
            return Assignment == Assignment.ItemActionWithItem;
        }

        public static ActionAssignment For(ActionType actionType)
        {
            return new(Assignment.Action, actionType, default, default);
        }

        public static ActionAssignment For(ItemActionType actionType)
        {
            return new(Assignment.ItemActionWithoutItem, default, actionType, default);
        }

        public static ActionAssignment For(ItemActionType actionType, EntityUid item)
        {
            return new(Assignment.ItemActionWithItem, default, actionType, item);
        }

        public bool Equals(ActionAssignment other)
        {
            return _actionType == other._actionType && _itemActionType == other._itemActionType && Equals(_item, other._item);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionAssignment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_actionType, _itemActionType, _item);
        }

        public override string ToString()
        {
            return $"{nameof(_actionType)}: {_actionType}, {nameof(_itemActionType)}: {_itemActionType}, {nameof(_item)}: {_item}, {nameof(Assignment)}: {Assignment}";
        }
    }

    public enum Assignment : byte
    {
        Action,
        ItemActionWithoutItem,
        ItemActionWithItem
    }
}
