using Content.Server.Utility;
using Content.Shared.Actions;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.TargetPoint
{
    public class DebugTargetPoint : ITargetPointAction
    {

        public bool UseMapPosition { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.UseMapPosition, "mapPos", true);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            if (UseMapPosition)
            {
                args.Performer.PopupMessageEveryone("Clicked map position " +
                                                    args.Target.ToMap(IoCManager.Resolve<IEntityManager>()));
            }
            else
            {
                args.Performer.PopupMessageEveryone("Clicked local position " +
                                                    args.Target);
            }

        }
    }
}
