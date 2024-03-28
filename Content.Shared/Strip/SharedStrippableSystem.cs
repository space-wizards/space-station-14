using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public abstract class SharedStrippableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrippingComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<StrippableComponent, CanDropDraggedEvent>(OnCanDrop);
        SubscribeLocalEvent<StrippableComponent, DragDropDraggedEvent>(OnDragDrop);
    }

    public (TimeSpan Time, bool Stealth) GetStripTimeModifiers(EntityUid user, EntityUid target, TimeSpan initialTime)
    {
        var userEv = new BeforeStripEvent(initialTime);
        RaiseLocalEvent(user, ref userEv);
        var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
        RaiseLocalEvent(target, ref ev);
        return (ev.Time, ev.Stealth);
    }

    private void OnDragDrop(EntityUid uid, StrippableComponent component, ref DragDropDraggedEvent args)
    {
        // If the user drags a strippable thing onto themselves.
        if (args.Handled || args.Target != args.User)
            return;

        StartOpeningStripper(args.User, (uid, component));
        args.Handled = true;
    }

    public virtual void StartOpeningStripper(EntityUid user, Entity<StrippableComponent> component, bool openInCombat = false)
    {

    }

    private void OnCanDropOn(EntityUid uid, StrippingComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= uid == args.User &&
                        HasComp<StrippableComponent>(args.Dragged) &&
                        HasComp<HandsComponent>(args.User);
    }

    private void OnCanDrop(EntityUid uid, StrippableComponent component, ref CanDropDraggedEvent args)
    {
        args.CanDrop |= args.Target == args.User &&
                        HasComp<StrippingComponent>(args.User) &&
                        HasComp<HandsComponent>(args.User);

        if (args.CanDrop)
            args.Handled = true;
    }
}
