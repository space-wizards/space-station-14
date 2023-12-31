using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Decals;

namespace Content.Shared.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class SharedPaintSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;


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
            if (HasComp<AppearanceComponent>(target))
            {
                RemComp<AppearanceComponent>(target);
            }
            EnsureComp<PaintedComponent>(target);
            AddComp<AppearanceComponent>(target);

            if (!TryComp(target, out PaintedComponent? paint))
            {
                return;
            }
            else
            {
                paint.Color = component.Color; // set the target color to the color specified in the spray paint yml.
                _audio.PlayPvs(component.Spray, uid);
                UpdateAppearance(target, paint);
                Dirty(target, paint);
                args.Handled = true;
                return;
            }
        }
        else
            return;

    }

    private bool TryPaint(EntityUid uid, PaintComponent component, EntityUid target, EntityUid actor)
    {
        if (HasComp<PaintedComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("paint-failure-painted", ("target", target)), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("paint-failure", ("target", target)), actor, actor, PopupType.Medium);
            return false;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target) && _solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            var quantity = solution.RemoveReagent(component.Reagent, component.ConsumptionUnit);
            if (quantity > 0)// checks quantity of solution is more than 0.
            {
                _popup.PopupClient(Loc.GetString("paint-success", ("target", target)), actor, actor, PopupType.Medium);
                return true;
            }
        }

        return false;
    }

    private void UpdateAppearance(EntityUid uid, PaintedComponent? component = null)
    {
    }
}
