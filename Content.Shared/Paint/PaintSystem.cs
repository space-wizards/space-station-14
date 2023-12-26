using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;

namespace Content.Shared.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class PaintSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintComponent, AfterInteractEvent>(OnInteract);
    }

    // When paint is used on an entity it will apply the color to the entity provided TryPaint returns true.
    private void OnInteract(EntityUid uid, PaintComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryPaint(uid, component, target, args.User))
        {
            EnsureComp<PaintedComponent>(target);

            if (!TryComp(target, out PaintedComponent? paint))
            {
                return;
            }
            else
            {
                paint.Color = component.Color; // trying to modify the painted color by having it match the paint color so that color can be set on the entity in yml (doesn't work).
                UpdateAppearance(uid, paint); // Maybe not needed.
                _audio.PlayPvs(component.Spray, uid);
                _popup.PopupClient(Loc.GetString("paint-success", ("target", target)), args.User, args.User, PopupType.Medium);
                args.Handled = true;
            }
        }
        else if (!TryPaint(uid, component, target, args.User))
        {
            if (!HasComp<PaintedComponent>(target))
            {
                _popup.PopupClient(Loc.GetString("paint-failure", ("target", target)), args.User, args.User, PopupType.Medium);
            }
            else
                _popup.PopupClient(Loc.GetString("paint-failure-painted", ("target", target)), args.User, args.User, PopupType.Medium);
        }
    }

    private bool TryPaint(EntityUid uid, PaintComponent component, EntityUid target, EntityUid actor)
    {
        if (HasComp<PaintedComponent>(target) || HasComp<HumanoidAppearanceComponent>(target))
        {
            return false;
        }
        if (!HasComp<HumanoidAppearanceComponent>(target) && _solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            var quantity = solution.RemoveReagent(component.Reagent, component.ConsumptionUnit);
            if (quantity > 0) // checks quantity of solution is more than 0.
                return true;
        }

        return false;
    }

    private void UpdateAppearance(EntityUid uid, PaintedComponent component)
    {
    }
}
