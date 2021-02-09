using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class DebugTargetEntity : ITargetEntityAction, ITargetEntityItemAction
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }

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
