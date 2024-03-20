using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Sprite;
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
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintRemoverComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PaintRemoverComponent, PaintRemoverDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PaintRemoverComponent, GetVerbsEvent<UtilityVerb>>(OnPaintRemoveVerb);
    }

    // When entity is painted, remove paint from that entity.
    private void OnInteract(EntityUid uid, PaintRemoverComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target || !HasComp<PaintedComponent>(target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.CleanDelay, new PaintRemoverDoAfterEvent(), uid, args.Target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        });
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, PaintRemoverComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out PaintedComponent? paint))
            return;

        paint.Enabled = false;
        _audio.PlayPredicted(component.Sound, target, args.User);
        _popup.PopupClient(Loc.GetString("paint-removed", ("target", target)), args.User, args.User, PopupType.Medium);
        _appearanceSystem.SetData(target, PaintVisuals.Painted, false);
        RemComp<PaintedComponent>(target);
        Dirty(target, paint);

        args.Handled = true;
    }

    private void OnPaintRemoveVerb(EntityUid uid, PaintRemoverComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var paintremovalText = Loc.GetString("paint-remove-verb");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {

                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.CleanDelay, new PaintRemoverDoAfterEvent(), uid, args.Target, uid)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    MovementThreshold = 1.0f,
                });
            },

            Text = paintremovalText
        };

        args.Verbs.Add(verb);
    }
}
