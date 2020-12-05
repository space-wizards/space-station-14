using System.Collections.Generic;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    /// Should be used only on player-controlled entities. Grants innate player actions.
    /// The actions are always granted regardless of status. You can use the methods on SharedActionsComponent
    /// to disable / enable the actions as desired.
    ///
    /// This is not required in order to give a player
    /// actions, it's just one way to do it (the other way is by explicitly calling
    /// SharedActionsComponent.Grant).
    /// </summary>
    [RegisterComponent]
    public class InnateActionsComponent : Component
    {
        public override string Name => "InnateActions";

        /// <summary>
        /// List of ItemActionTypes that will be granted when this item is picked up.
        /// </summary>
        public IEnumerable<ActionType> Actions => _actions;
        private List<ActionType> _actions;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _actions,"actions", new List<ActionType>());
        }

        protected override void Startup()
        {
            if (!Owner.TryGetComponent<SharedActionsComponent>(out var actionsComponent)) return;
            foreach (var actionType in Actions)
            {
                actionsComponent.Grant(actionType);
            }
        }
    }
}
