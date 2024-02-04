using Content.Shared.Popups;
using Content.Shared.Paint;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Content.Shared.SubFloor;

namespace Content.Server.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class PaintSystem : SharedPaintSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PaintComponent, PaintDoAfterEvent>(OnPaint);
    }

    private void OnInteract(EntityUid uid, PaintComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, 2f, new PaintDoAfterEvent(), args.Used, target: args.Target, used: args.Used)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            return;
    }

    private void OnPaint(Entity<PaintComponent> entity, ref PaintDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (HasComp<PaintedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("paint-failure-painted", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!entity.Comp.Blacklist?.IsValid(target, EntityManager) != true || HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("paint-failure", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            return;
        }

        if (TryPaint(entity, target))
        {
            EnsureComp<PaintedComponent>(target, out PaintedComponent? paint);
            EnsureComp<AppearanceComponent>(target);

            paint.Color = entity.Comp.Color; // set the target color to the color specified in the spray paint yml.
            _audio.PlayPvs(entity.Comp.Spray, entity);
            paint.Enabled = true;
            _popup.PopupEntity(Loc.GetString("paint-success", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            _appearanceSystem.SetData(target, PaintVisuals.Painted, true);
            Dirty(target, paint);
            args.Handled = true;
            return;
        }
        if (args.Used == null)
            return;

        if (!TryPaint(entity, target))
            _popup.PopupEntity(Loc.GetString("paint-empty", ("used", args.Used)), args.User, args.User, PopupType.Medium);
        return;

    }

    private bool TryPaint(Entity<PaintComponent> reagent, EntityUid target)
    {
        if (HasComp<PaintedComponent>(target))
            return false;

        if (HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target))
            return false;

        if (_solutionContainer.TryGetSolution(reagent.Owner, reagent.Comp.Solution, out _, out var solution))
        {
            var quantity = solution.RemoveReagent(reagent.Comp.Reagent, reagent.Comp.ConsumptionUnit);
            if (quantity > 0)// checks quantity of solution is more than 0.
                return true;

            if (quantity < 1)
                return false;
        }
        return false;
    }
}
