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
    public class DebugTargetEntity : ITargetEntityAction, ITargetEntityItemAction
    {
        public void DoTargetEntityAction(TargetEntityItemActionEventArgs args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            args.Performer.PopupMessageEveryone(entMan.GetComponent<MetaDataComponent>(args.Item).EntityName + ": Clicked " +
                                                entMan.GetComponent<MetaDataComponent>(args.Target).EntityName);
        }

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked " + IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(args.Target).EntityName);
        }
    }
}
