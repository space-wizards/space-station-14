using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.TargetPoint
{
    [UsedImplicitly]
    public class DebugTargetPoint : ITargetPointAction
    {
        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked local position " +
                                                args.Target);
        }
    }
}
