using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DebugTargetEntity : ITargetEntityAction, ITargetEntityItemAction
    {
        public void DoTargetEntityAction(TargetEntityItemActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(args.Item.Name + ": Clicked " +
                                                args.Target.Name);
        }

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked " +
                                                args.Target.Name);
        }
    }
}
