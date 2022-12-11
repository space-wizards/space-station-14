using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public abstract class SharedStrippableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrippingComponent, CanDropOnEvent>(OnCanDropOn);
        SubscribeLocalEvent<StrippableComponent, CanDropEvent>(OnCanDrop);
        SubscribeLocalEvent<StrippableComponent, DragDropEvent>(OnDragDrop);
    }

    private void OnDragDrop(EntityUid uid, StrippableComponent component, ref DragDropEvent args)
    {
        StartOpeningStripper(args.User, component);
    }

    public virtual void StartOpeningStripper(EntityUid user, StrippableComponent component, bool openInCombat = false)
    {

    }

    private void OnCanDropOn(EntityUid uid, StrippingComponent component, ref CanDropOnEvent args)
    {
        args.Handled = true;
        args.CanDrop |= uid != args.Dragged &&
                        uid == args.User &&
                        HasComp<StrippableComponent>(args.Dragged) &&
                        HasComp<SharedHandsComponent>(args.User);
    }

    private void OnCanDrop(EntityUid uid, StrippableComponent component, ref CanDropEvent args)
    {
        args.Handled = true;
        args.CanDrop |= args.Target != uid &&
                        args.Target == args.User &&
                        HasComp<StrippingComponent>(args.User) &&
                        HasComp<SharedHandsComponent>(args.User);
    }
}
