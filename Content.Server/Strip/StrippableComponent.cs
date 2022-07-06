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
        /// How long it takes to open the strip menu.
        /// This should be relatively short so it's not a hassle
        /// but so it also doesn't open immediately during melee combat
        /// </summary>
        [ViewVariables]
        [DataField("openDelay")]
        public float OpenDelay = 1f;

        /// <summary>
        /// The strip delay for hands.
        /// </summary>
        [ViewVariables]
        [DataField("handDelay")]
        public float HandStripDelay = 3f;

        public override bool Drop(DragDropEvent args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<StrippableSystem>().StartOpeningStripper(args.User, this);
            return true;
        }

        public Dictionary<EntityUid, CancellationTokenSource> CancelTokens = new();
    }
}
