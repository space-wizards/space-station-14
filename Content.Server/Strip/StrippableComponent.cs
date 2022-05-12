using Content.Server.UserInterface;
using Content.Shared.DragDrop;
using Content.Shared.Strip.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    [Friend(typeof(StrippableSystem))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        [ViewVariables]
        [DataField("delay")]
        public float StripDelay = 6f;

        public override bool Drop(DragDropEvent args)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(args.User, out ActorComponent? actor)) return false;

            Owner.GetUIOrNull(StrippingUiKey.Key)?.Open(actor.PlayerSession);
            return true;
        }
    }
}
