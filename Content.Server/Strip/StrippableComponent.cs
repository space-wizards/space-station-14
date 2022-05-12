using System.Threading;
using Content.Shared.DragDrop;
using Content.Shared.Strip.Components;

namespace Content.Server.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    [Friend(typeof(StrippableSystem))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        [ViewVariables]
        [DataField("openDelay")]
        public float OpenDelay = 4f;

        [ViewVariables]
        [DataField("delay")]
        public float StripDelay = 2f;

        public override bool Drop(DragDropEvent args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<StrippableSystem>().StartOpeningStripper(args.User, this);
            return true;
        }

        public Dictionary<EntityUid, CancellationTokenSource> CancelTokens = new();
    }
}
