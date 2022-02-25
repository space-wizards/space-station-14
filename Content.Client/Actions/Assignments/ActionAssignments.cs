using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.Actions.Assignments
{
    /// <summary>
    /// Tracks and manages the hotbar assignments for actions.
    /// </summary>
    public sealed class ActionAssignments
    {
        // the slots and assignments fields hold client's assignments (what action goes in what slot),
        // which are completely client side and independent of what actions they've actually been granted and
        // what item the action is actually for.

        /// <summary>
        /// x = hotbar number, y = slot of that hotbar (index 0 corresponds to the one labeled "1",
        /// index 9 corresponds to the one labeled "0"). Essentially the inverse of _assignments.
        /// </summary>
        private readonly ActionAssignment?[,] _slots;

        /// <summary>
        /// Hotbar and slot assignment for each action type (slot index 0 corresponds to the one labeled "1",
        /// slot index 9 corresponds to the one labeled "0"). The key corresponds to an index in the _slots array.
        /// The value is a list because actions can be assigned to multiple slots. Even if an action type has not been granted,
        /// it can still be assigned to a slot. Essentially the inverse of _slots.
        /// There will be no entry if there is no assignment (no empty lists in this dict)
        /// </summary>
        private readonly Dictionary<ActionAssignment, List<(byte Hotbar, byte Slot)>> _assignments;

        /// <summary>
        /// Actions which have been manually cleared by the user, thus should not
        /// auto-populate.
        /// </summary>
        private readonly HashSet<ActionType> _preventAutoPopulate = new();
        private readonly Dictionary<EntityUid, HashSet<ItemActionType>> _preventAutoPopulateItem = new();

        private readonly byte _numHotbars;
        private readonly byte _numSlots;

        public ActionAssignments(byte numHotbars, byte numSlots)
        {
            _numHotbars = numHotbars;
            _numSlots = numSlots;
            _assignments = new Dictionary<ActionAssignment, List<(byte Hotbar, byte Slot)>>();
            _slots = new ActionAssignment?[numHotbars, numSlots];
        }

        /// <summary>
        /// Updates the assignments based on the current states of all the actions.
        /// Newly-granted actions or item actions which don't have an assignment will be assigned a slot
        /// automatically (unless they've been manually cleared). Item-based actions
        /// which no longer have an associated state will be decoupled from their item.
        /// </summary>
        public void Reconcile(byte currentHotbar, IReadOnlyDictionary<ActionType, ActionState> actionStates,
            IReadOnlyDictionary<EntityUid, Dictionary<ItemActionType, ActionState>> itemActionStates,
            bool actionMenuLocked)
        {
            // if we've been granted any actions which have no assignment to any hotbar, we must auto-populate them
            // into the hotbar so the user knows about them.
            // We fill their current hotbar first, rolling over to the next open slot on the next hotbar.
            foreach (var actionState in actionStates)
            {
                var assignment = ActionAssignment.For(actionState.Key);
                if (actionState.Value.Enabled && !_assignments.ContainsKey(assignment))
                {
                    AutoPopulate(assignment, currentHotbar, false);
                }
            }

            foreach (var (item, itemStates) in itemActionStates)
            {
                foreach (var itemActionState in itemStates)
                {
                    // unlike regular actions, we DO actually show user their new item action even when it's disabled.
                    // this allows them to instantly see when an action may be possible that is provided by an item but
                    // something is preventing it
                    // Note that we are checking if there is an explicit assignment for this item action + item,
                    // we will determine during auto-population if we should tie the item to an existing "item action only"
                    // assignment
                    var assignment = ActionAssignment.For(itemActionState.Key, item);
                    if (!_assignments.ContainsKey(assignment))
                    {
                        AutoPopulate(assignment, currentHotbar, false);
                    }
                }
            }

            // We need to figure out which current item action assignments we had
            // which once had an associated item but have been revoked (based on our newly provided action states)
            // so we can dissociate them from the item. If the provided action states do not
            // have a state for this action type + item, we can assume that the action has been revoked for that item.
            var assignmentsWithoutItem = new List<KeyValuePair<ActionAssignment, List<(byte Hotbar, byte Slot)>>>();
            foreach (var assignmentEntry in _assignments)
            {
                if (!assignmentEntry.Key.TryGetItemActionWithItem(out var actionType, out var item))
                {
                    continue;
                }

                // we have this assignment currently tied to an item,
                // check if it no longer has an associated item in our dict of states
                if (itemActionStates.TryGetValue(item, out var states))
                {
                    if (states.ContainsKey(actionType))
                    {
                        // we have a state for this item + action type so we won't
                        // remove the item from the assignment
                        continue;
                    }
                }

                assignmentsWithoutItem.Add(assignmentEntry);
            }

            // reassign without the item for each assignment we found that no longer has an associated item
            foreach (var (assignment, slots) in assignmentsWithoutItem)
            {
                foreach (var (hotbar, slot) in slots)
                {
                    if (!assignment.TryGetItemActionWithItem(out var actionType, out _))
                    {
                        continue;
                    }

                    if (actionMenuLocked)
                    {
                        AssignSlot(hotbar, slot, ActionAssignment.For(actionType));
                    }
                    else
                    {
                        ClearSlot(hotbar, slot, false);
                    }
                }
            }

            // Additionally, we must find items which have no action states at all in our newly provided states so
            // we can assume their item was unequipped and reset them to allow auto-population.
            var itemsWithoutState = _preventAutoPopulateItem.Keys.Where(item => !itemActionStates.ContainsKey(item));
            foreach (var toRemove in itemsWithoutState)
            {
                _preventAutoPopulateItem.Remove(toRemove);
            }
        }

        /// <summary>
        /// Assigns the indicated hotbar slot to the specified action type.
        /// </summary>
        /// <param name="hotbar">hotbar whose slot is being assigned</param>
        /// <param name="slot">slot of the hotbar to assign to (0 = the slot labeled 1, 9 = the slot labeled 0)</param>
        /// <param name="actionType">action to assign to the slot</param>
        public void AssignSlot(byte hotbar, byte slot, ActionAssignment actionType)
        {
            ClearSlot(hotbar, slot, false);
            _slots[hotbar, slot] = actionType;
            if (_assignments.TryGetValue(actionType, out var slotList))
            {
                slotList.Add((hotbar, slot));
            }
            else
            {
                var newList = new List<(byte Hotbar, byte Slot)> { (hotbar, slot) };
                _assignments[actionType] = newList;
            }
        }

        /// <summary>
        /// Clear the assignment from the indicated slot.
        /// </summary>
        /// <param name="hotbar">hotbar whose slot is being cleared</param>
        /// <param name="slot">slot of the hotbar to clear (0 = the slot labeled 1, 9 = the slot labeled 0)</param>
        /// <param name="preventAutoPopulate">if true, the action assigned to this slot
        /// will be prevented from being auto-populated in the future when it is newly granted.
        /// Item actions will automatically be allowed to auto populate again
        /// when their associated item are unequipped. This ensures that items that are newly
        /// picked up will always present their actions to the user even if they had earlier been cleared.
        /// </param>
        public void ClearSlot(byte hotbar, byte slot, bool preventAutoPopulate)
        {
            // remove this particular assignment from our data structures
            // (keeping in mind something can be assigned multiple slots)
            var currentAction = _slots[hotbar, slot];

            if (!currentAction.HasValue)
            {
                return;
            }

            if (preventAutoPopulate)
            {
                var assignment = currentAction.Value;

                if (assignment.TryGetAction(out var actionType))
                {
                    _preventAutoPopulate.Add(actionType);
                }
                else if (assignment.TryGetItemActionWithItem(out var itemActionType, out var item))
                {
                    if (!_preventAutoPopulateItem.TryGetValue(item, out var actionTypes))
                    {
                        actionTypes = new HashSet<ItemActionType>();
                        _preventAutoPopulateItem[item] = actionTypes;
                    }

                    actionTypes.Add(itemActionType);
                }
            }

            var assignmentList = _assignments[currentAction.Value];
            assignmentList = assignmentList.Where(a => a.Hotbar != hotbar || a.Slot != slot).ToList();
            if (!assignmentList.Any())
            {
                _assignments.Remove(currentAction.Value);
            }
            else
            {
                _assignments[currentAction.Value] = assignmentList;
            }

            _slots[hotbar, slot] = null;
        }

        /// <summary>
        /// Finds the next open slot the action can go in and assigns it there,
        /// starting from the currently selected hotbar.
        /// Does not update any UI elements, only updates the assignment data structures.
        /// </summary>
        /// <param name="force">if true, will force the assignment to occur
        /// regardless of whether this assignment has been prevented from auto population
        /// via ClearSlot's preventAutoPopulate parameter. If false, will have no effect
        /// if this assignment has been prevented from auto population.</param>
        public void AutoPopulate(ActionAssignment toAssign, byte currentHotbar, bool force = true)
        {
            if (ShouldPreventAutoPopulate(toAssign, force))
            {
                return;
            }

            // if the assignment to make is an item action with an associated item,
            // then first look for currently assigned item actions without an item, to replace with this
            // assignment
            if (toAssign.TryGetItemActionWithItem(out var actionType, out var _))
            {
                if (_assignments.TryGetValue(ActionAssignment.For(actionType),
                    out var possibilities))
                {
                    // use the closest assignment to current hotbar
                    byte hotbar = 0;
                    byte slot = 0;
                    var minCost = int.MaxValue;
                    foreach (var possibility in possibilities)
                    {
                        var cost = possibility.Slot + _numSlots * (currentHotbar >= possibility.Hotbar
                            ? currentHotbar - possibility.Hotbar
                            : _numHotbars - currentHotbar + possibility.Hotbar);
                        if (cost < minCost)
                        {
                            hotbar = possibility.Hotbar;
                            slot = possibility.Slot;
                            minCost = cost;
                        }
                    }

                    if (minCost != int.MaxValue)
                    {
                        AssignSlot(hotbar, slot, toAssign);
                        return;
                    }
                }
            }

            for (byte hotbarOffset = 0; hotbarOffset < _numHotbars; hotbarOffset++)
            {
                for (byte slot = 0; slot < _numSlots; slot++)
                {
                    var hotbar = (byte) ((currentHotbar + hotbarOffset) % _numHotbars);
                    var slotAssignment = _slots[hotbar, slot];
                    if (slotAssignment.HasValue)
                    {
                        // if the assignment in this slot is an item action without an associated item,
                        // then tie it to the current item if we are trying to auto populate an item action.
                        if (toAssign.Assignment == Assignment.ItemActionWithItem &&
                            slotAssignment.Value.Assignment == Assignment.ItemActionWithoutItem)
                        {
                            AssignSlot(hotbar, slot, toAssign);
                            return;
                        }

                        continue;
                    }

                    // slot's empty, assign
                    AssignSlot(hotbar, slot, toAssign);
                    return;
                }
            }
            // there was no empty slot
        }

        private bool ShouldPreventAutoPopulate(ActionAssignment assignment, bool force)
        {
            if (force)
            {
                return false;
            }

            if (assignment.TryGetAction(out var actionType))
            {
                return _preventAutoPopulate.Contains(actionType);
            }

            if (assignment.TryGetItemActionWithItem(out var itemActionType, out var item))
            {
                return _preventAutoPopulateItem.TryGetValue(item,
                    out var itemActionTypes) && itemActionTypes.Contains(itemActionType);
            }

            return false;
        }

        /// <summary>
        /// Gets the assignment to the indicated slot if there is one.
        /// </summary>
        public ActionAssignment? this[in byte hotbar, in byte slot] => _slots[hotbar, slot];

        /// <returns>true if we have the assignment assigned to some slot</returns>
        public bool HasAssignment(ActionAssignment assignment)
        {
            return _assignments.ContainsKey(assignment);
        }
    }
}
