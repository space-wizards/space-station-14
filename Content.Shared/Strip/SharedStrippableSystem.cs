using Content.Shared.CombatMode;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public abstract class SharedStrippableSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrippingComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<StrippableComponent, CanDropDraggedEvent>(OnCanDrop);
        SubscribeLocalEvent<StrippableComponent, DragDropDraggedEvent>(OnDragDrop);
        SubscribeLocalEvent<StrippableComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(EntityUid uid, StrippableComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || args.Target == args.User)
            return;

        if (TryOpenStrippingUi(args.User, (uid, component)))
            args.Handled = true;
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

        if (TryOpenStrippingUi(args.User, (uid, component)))
            args.Handled = true;
    }

    public bool TryOpenStrippingUi(EntityUid user, Entity<StrippableComponent> target, bool openInCombat = false)
    {
        if (!openInCombat && TryComp<CombatModeComponent>(user, out var mode) && mode.IsInCombatMode)
            return false;

        if (!HasComp<StrippingComponent>(user))
            return false;

        _ui.OpenUi(target.Owner, StrippingUiKey.Key, user);
        return true;
    }

    private void OnCanDropOn(EntityUid uid, StrippingComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= uid == args.User &&
                        HasComp<StrippableComponent>(args.Dragged) &&
                        HasComp<HandsComponent>(args.User) &&
                        HasComp<StrippingComponent>(args.User);
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
