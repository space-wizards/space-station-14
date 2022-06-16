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
        [ViewVariables]
        [DataField("openDelay")]
        public float OpenDelay = 1f;

        /// <summary>
        /// The default strip delay to default to if it doesn't meet params or not specified.
        /// </summary>
        [ViewVariables]
        [DataField("defaultDelay")]
        public float DefaultStripDelay = 3f;

        /// <summary>
        /// Delay for important objects that should take awhile to steal
        /// Jumpsuit, suit, back, belt and ID
        /// </summary>
        [ViewVariables]
        [DataField("importantDelay")]
        public float ImportantDelay = 5f;

        /// <summary>
        /// Delay for not so important objects that shouldn't take too long to steal
        /// Pockets, ears, eyes, shoes and suit storage.
        /// </summary>
        [ViewVariables]
        [DataField("lowPriorityDelay")]
        public float LowPriorityDelay = 2f;

        /// <summary>
        /// Store the value for the strip delay
        /// </summary>
        [ViewVariables]
        public float StripDelay;

        public override bool Drop(DragDropEvent args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<StrippableSystem>().StartOpeningStripper(args.User, this);
            return true;
        }

        public Dictionary<EntityUid, CancellationTokenSource> CancelTokens = new();
    }
}
