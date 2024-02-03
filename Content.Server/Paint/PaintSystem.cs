using Content.Shared.Popups;
using Content.Shared.Paint;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnInteract(Entity<PaintComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!TryComp(args.Used, out PaintComponent? component))
            return;

        if (TryPaint(entity, target))
        {
            EnsureComp<PaintedComponent>(target, out PaintedComponent? paint);
            EnsureComp<AppearanceComponent>(target);

            paint.Color = component.Color; // set the target color to the color specified in the spray paint yml.
            _audio.PlayPvs(component.Spray, args.Used);
            paint.Enabled = true;
            _popup.PopupEntity(Loc.GetString("paint-success", ("target", target)), args.User, args.User, PopupType.Medium);
            _appearanceSystem.SetData(target, PaintVisuals.Painted, true);
            Dirty(target, paint);
            args.Handled = true;
            return;
        }

        if (!TryPaint(entity, target))
            _popup.PopupEntity(Loc.GetString("paint-failure", ("target", target)), args.User, args.User, PopupType.Medium);
        return;
    }

    private bool TryPaint(Entity<PaintComponent> reagent, EntityUid target)
    {
        if (HasComp<PaintedComponent>(target))
            return false;

        if (HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target) || HasComp<NoPaintShaderComponent>(target) )
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
