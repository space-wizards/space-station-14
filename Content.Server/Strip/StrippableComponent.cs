using System.Threading;
using Content.Shared.DragDrop;
using Content.Shared.Strip.Components;

namespace Content.Server.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    [Access(typeof(StrippableSystem))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        /// <summary>
        /// The strip delay for hands.
        /// </summary>
        [DataField("handDelay")]
        public float HandStripDelay = 4f;

        public override bool Drop(DragDropEvent args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<StrippableSystem>().StartOpeningStripper(args.User, this);
            return true;
        }

        public Dictionary<EntityUid, CancellationTokenSource> CancelTokens = new();
    }
}
