﻿using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Provides access to all configured actions by action type.
    /// </summary>
    public class ActionManager
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        private Dictionary<ActionType, ActionPrototype> _typeToAction;
        private Dictionary<ItemActionType, ItemActionPrototype> _typeToItemAction;

        public void Initialize()
        {
            _typeToAction = new Dictionary<ActionType, ActionPrototype>();
            foreach (var action in _prototypeManager.EnumeratePrototypes<ActionPrototype>())
            {
                if (!_typeToAction.TryAdd(action.ActionType, action))
                {
                    Logger.ErrorS("action",
                        "Found action with duplicate actionType {0} - all actions must have" +
                        " a unique actionType, this one will be skipped", action.ActionType);
                }
            }

            _typeToItemAction = new Dictionary<ItemActionType, ItemActionPrototype>();
            foreach (var action in _prototypeManager.EnumeratePrototypes<ItemActionPrototype>())
            {
                if (!_typeToItemAction.TryAdd(action.ActionType, action))
                {
                    Logger.ErrorS("action",
                        "Found itemAction with duplicate actionType {0} - all actions must have" +
                        " a unique actionType, this one will be skipped", action.ActionType);
                }
            }
        }

        /// <returns>all action prototypes of all types</returns>
        public IEnumerable<BaseActionPrototype> EnumerateActions()
        {
            return _typeToAction.Values.Concat<BaseActionPrototype>(_typeToItemAction.Values);
        }


        /// <summary>
        /// Tries to get the action of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(ActionType actionType, out ActionPrototype action)
        {
            return _typeToAction.TryGetValue(actionType, out action);
        }

        /// <summary>
        /// Tries to get the item action of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(ItemActionType actionType, out ItemActionPrototype action)
        {
            return _typeToItemAction.TryGetValue(actionType, out action);
        }
    }
}
