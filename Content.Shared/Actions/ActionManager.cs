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
        }

        /// <summary>
        /// Tries to get the action of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(ActionType actionType, out ActionPrototype action)
        {
            return _typeToAction.TryGetValue(actionType, out action);
        }
    }
}
