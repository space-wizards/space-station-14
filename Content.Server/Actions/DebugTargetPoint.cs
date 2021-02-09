using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class DebugTargetPoint : ITargetPointAction, ITargetPointItemAction
    {
        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public void DoTargetPointAction(TargetPointItemActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(args.Item.Name + ": Clicked local position " +
                                                args.Target);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked local position " +
                                                args.Target);
        }
    }
}
