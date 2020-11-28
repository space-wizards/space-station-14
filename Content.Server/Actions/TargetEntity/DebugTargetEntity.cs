using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.TargetEntity
{
    [UsedImplicitly]
    public class DebugTargetEntity : ITargetEntityAction
    {

        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public void DoTargetEntityAction(TargetEntitytActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone("Clicked " +
                                                args.Target.Name);
        }
    }
}
