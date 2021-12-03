using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DebugTargetPoint : ITargetPointAction, ITargetPointItemAction
    {
        public void DoTargetPointAction(TargetPointItemActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(args.Item).EntityName + ": Clicked local position " +
                                                args.Target);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked local position " +
                                                args.Target);
        }
    }
}
