

using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    /// Should be used only on items. Manages actions provided by an item.
    ///
    /// Currently all it does is grant specific item actions when picked up (they will
    /// be revoked automatically by SharedActionsComponent when dropped). Eventually it could be
    /// used to support more complex use cases or scrapped entirely if a better design for item
    /// actions is worked out.
    /// </summary>
    [RegisterComponent]
    public class ItemActionsComponent : Component, IEquippedHand, IEquipped
    {
        public override string Name => "ItemActions";

        /// <summary>
        /// List of ItemActionTypes that will be granted when this item is picked up.
        /// </summary>
        public IEnumerable<ItemActionType> Actions => _actions;
        private List<ItemActionType> _actions;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _actions,"actions", new List<ItemActionType>());
        }

        public void EquippedHand(EquippedHandEventArgs eventArgs)
        {
            Grant(eventArgs.User);
        }

        public void Equipped(EquippedEventArgs eventArgs)
        {
            Grant(eventArgs.User);
        }

        private void Grant(IEntity user)
        {
            if (!user.TryGetComponent<SharedActionsComponent>(out var actionsComponent)) return;
            foreach (var actionType in Actions)
            {
                actionsComponent.Grant(actionType, Owner, true);
            }
        }
    }
}
