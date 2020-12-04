using System;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.Actions
{
    public struct ActionAssignment
    {
        public readonly ActionType? ActionType;
        public readonly ItemActionType? ItemActionType;
        public readonly EntityUid? Item;

        public Assignment Assignment => ActionType.HasValue ? Assignment.Action :
            Item.HasValue ? Assignment.ItemActionWithItem : Assignment.ItemActionWithoutItem;

        private ActionAssignment(ActionType? actionType, ItemActionType? itemActionType, EntityUid? item)
        {
            ActionType = actionType;
            ItemActionType = itemActionType;
            Item = item;
        }

        public static ActionAssignment For(ActionType actionType)
        {
            return new ActionAssignment(actionType, null, null);
        }

        public static ActionAssignment For(ItemActionType actionType)
        {
            return new ActionAssignment(null, actionType, null);
        }

        public static ActionAssignment For(ItemActionType actionType, EntityUid item)
        {
            return new ActionAssignment(null, actionType, item);
        }

        public bool Equals(ActionAssignment other)
        {
            return ActionType == other.ActionType && ItemActionType == other.ItemActionType && Nullable.Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionAssignment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, ItemActionType, Item);
        }

        public override string ToString()
        {
            return $"{nameof(ActionType)}: {ActionType}, {nameof(ItemActionType)}: {ItemActionType}, {nameof(Item)}: {Item}, {nameof(Assignment)}: {Assignment}";
        }
    }

    public enum Assignment
    {
        Action,
        ItemActionWithoutItem,
        ItemActionWithItem
    }
}
