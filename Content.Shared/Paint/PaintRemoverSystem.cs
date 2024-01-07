using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;


namespace Content.Shared.Paint;

/// <summary>
/// Removes paint from an entity.
/// </summary>
public sealed class PaintRemoverSystem : SharedPaintSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintRemoverComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PaintRemoverComponent, PaintRemoverDoAfterEvent>(OnDoAfter);
    }

    // When entity is painted, remove paint from that entity.
    private void OnInteract(EntityUid uid, PaintRemoverComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target || !HasComp<PaintedComponent>(target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.CleanDelay, new PaintRemoverDoAfterEvent(), uid, target: args.Target, used: uid));
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, PaintRemoverComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out PaintedComponent? paint))
            return;

        if (_timing.IsFirstTimePredicted)
        {
            paint.Enabled = false;
            _audio.PlayPvs(component.Sound, target);
            _popup.PopupClient(Loc.GetString("you clean off the paint", ("target", target)), args.User, args.User, PopupType.Medium);
            RemComp<PaintedComponent>(target);
            Dirty(target, paint);

        }

        args.Handled = true;
    }
}
