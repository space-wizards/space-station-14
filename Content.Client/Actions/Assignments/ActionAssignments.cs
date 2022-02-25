using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using System.Linq;

namespace Content.Client.Actions.Assignments
{
    /// <summary>
    /// Tracks and manages the hotbar assignments for actions.
    /// </summary>
    [DataDefinition]
    public sealed class ActionAssignments
    {
        // the slots and assignments fields hold client's assignments (what action goes in what slot),
        // which are completely client side and independent of what actions they've actually been granted and
        // what item the action is actually for.

        /// <summary>
        /// x = hotbar number, y = slot of that hotbar (index 0 corresponds to the one labeled "1",
        /// index 9 corresponds to the one labeled "0"). Essentially the inverse of _assignments.
        /// </summary>
        private readonly ActionType?[,] _slots;

        /// <summary>
        /// Hotbar and slot assignment for each action type (slot index 0 corresponds to the one labeled "1",
        /// slot index 9 corresponds to the one labeled "0"). The key corresponds to an index in the _slots array.
        /// The value is a list because actions can be assigned to multiple slots. Even if an action type has not been granted,
        /// it can still be assigned to a slot. Essentially the inverse of _slots.
        /// There will be no entry if there is no assignment (no empty lists in this dict)
        /// </summary>
        [DataField("assignments")]
        public readonly Dictionary<ActionType, List<(byte Hotbar, byte Slot)>> Assignments = new();

        /// <summary>
        /// Actions which have been manually cleared by the user, thus should not
        /// auto-populate.
        /// </summary>
        public readonly SortedSet<ActionType> PreventAutoPopulate = new();

        private readonly byte _numHotbars;
        private readonly byte _numSlots;

        public ActionAssignments(byte numHotbars, byte numSlots)
        {
            _numHotbars = numHotbars;
            _numSlots = numSlots;
            _slots = new ActionType?[numHotbars, numSlots];
        }

        public bool Remove(ActionType action) => Replace(action, null);

        internal bool Replace(ActionType action, ActionType? newAction)
        {
            if (!Assignments.Remove(action, out var assigns))
                return false;

            if (newAction != null)
                Assignments[newAction] = assigns;

            foreach (var (bar, slot) in assigns)
            {
                _slots[bar, slot] = newAction;
            }

            return true;
        }

        /// <summary>
        /// Assigns the indicated hotbar slot to the specified action type.
        /// </summary>
        /// <param name="hotbar">hotbar whose slot is being assigned</param>
        /// <param name="slot">slot of the hotbar to assign to (0 = the slot labeled 1, 9 = the slot labeled 0)</param>
        /// <param name="actionType">action to assign to the slot</param>
        public void AssignSlot(byte hotbar, byte slot, ActionType actionType)
        {
            ClearSlot(hotbar, slot, false);
            _slots[hotbar, slot] = actionType;
            if (Assignments.TryGetValue(actionType, out var slotList))
            {
                slotList.Add((hotbar, slot));
            }
            else
            {
                var newList = new List<(byte Hotbar, byte Slot)> { (hotbar, slot) };
                Assignments[actionType] = newList;
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

            if (currentAction == null)
                return;

            if (preventAutoPopulate)
                PreventAutoPopulate.Add(currentAction);

            var assignmentList = Assignments[currentAction];
            assignmentList = assignmentList.Where(a => a.Hotbar != hotbar || a.Slot != slot).ToList();
            if (!assignmentList.Any())
            {
                Assignments.Remove(currentAction);
            }
            else
            {
                Assignments[currentAction] = assignmentList;
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
        public void AutoPopulate(ActionType toAssign, byte currentHotbar, bool force = true)
        {
            if (!force && PreventAutoPopulate.Contains(toAssign))
                return;

            for (byte hotbarOffset = 0; hotbarOffset < _numHotbars; hotbarOffset++)
            {
                for (byte slot = 0; slot < _numSlots; slot++)
                {
                    var hotbar = (byte) ((currentHotbar + hotbarOffset) % _numHotbars);
                    var slotAssignment = _slots[hotbar, slot];

                    if (slotAssignment != null)
                        continue;

                    AssignSlot(hotbar, slot, toAssign);
                    return;
                }
            }
            // there was no empty slot
        }

        /// <summary>
        /// Gets the assignment to the indicated slot if there is one.
        /// </summary>
        public ActionType? this[in byte hotbar, in byte slot] => _slots[hotbar, slot];

        /// <returns>true if we have the assignment assigned to some slot</returns>
        public bool HasAssignment(ActionType assignment)
        {
            return Assignments.ContainsKey(assignment);
        }
    }
}
