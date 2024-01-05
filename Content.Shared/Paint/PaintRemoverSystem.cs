using Content.Shared.Popups;
using Content.Shared.Interaction;


namespace Content.Shared.Paint;

/// <summary>
/// Removes paint from an entity.
/// </summary>
public sealed class PaintRemoverSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPaintSystem _paint = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintRemoverComponent, AfterInteractEvent>(OnInteract);
    }

    // When entity is painted, remove paint from that entity.
    private void OnInteract(EntityUid uid, PaintRemoverComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target || !HasComp<PaintedComponent>(target))
            return;


        if (!TryComp(target, out PaintedComponent? paint))
            return;


        if (HasComp<AppearanceComponent>(target))
            RemComp<AppearanceComponent>(target);

        AddComp<AppearanceComponent>(target);
        paint.Enabled = false;
        _paint.UpdateAppearance(target, paint);
        Dirty(target, paint);
        _popup.PopupClient(Loc.GetString("you clean off the paint", ("target", target)), args.User, args.User, PopupType.Medium);
        args.Handled = true;
    }
}
