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
            if (args.Item.TryGetComponent(out InternalsProviderComponent? provider))
                return EntitySystem.Get<InternalsProviderSystem>().ToggleInternals(args.Item.Uid, provider!, new ToggleInternalsEvent(args.ToggledOn));

            if (args.Item.TryGetComponent(out InternalsSuitComponent? suit))
                return EntitySystem.Get<InternalsSuitSystem>().ToggleInternals(args.Item.Uid, suit!, new ToggleInternalsEvent(args.ToggledOn));

            return false;
        }
    }
}
