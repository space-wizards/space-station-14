using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Provides access to all configured actions by action type.
    /// </summary>
    public sealed class ActionManager
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<ActionType, ActionPrototype> _typeToAction = new();
        private readonly Dictionary<ItemActionType, ItemActionPrototype> _typeToItemAction = new();

        public void Initialize()
        {
            foreach (var action in _prototypeManager.EnumeratePrototypes<ActionPrototype>())
            {
                if (!_typeToAction.TryAdd(action.ActionType, action))
                {
                    Logger.ErrorS("action",
                        "Found action with duplicate actionType {0} - all actions must have" +
                        " a unique actionType, this one will be skipped", action.ActionType);
                }
            }

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
        public bool TryGet(ActionType actionType, [NotNullWhen(true)] out ActionPrototype? action)
        {
            return _typeToAction.TryGetValue(actionType, out action);
        }

        /// <summary>
        /// Tries to get the item action of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(ItemActionType actionType, [NotNullWhen(true)] out ItemActionPrototype? action)
        {
            return _typeToItemAction.TryGetValue(actionType, out action);
        }
    }
}
