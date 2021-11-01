using Content.Server.Atmos.Components;
using Content.Server.Internals;
using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ToggleInternalsAction : IToggleItemAction
    {
        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            var toggleEvent = new ToggleInternalsEvent(args.ToggledOn, false, args.ItemActions);
            args.Item.EntityManager.EventBus.RaiseLocalEvent(args.Item.Uid, toggleEvent);

            return toggleEvent.Handled;
        }
    }
}
