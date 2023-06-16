using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Glue;
using Content.Shared.Interaction;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;

namespace Content.Server.Glue;

public sealed class GlueSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlueComponent, AfterInteractEvent>(OnInteract);
    }

    // When glue bottle is used on item it will apply the glued and unremoveable components.
    private void OnInteract(EntityUid uid, GlueComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryComp<DrinkComponent>(uid, out var drink) && !drink.Opened)
        {
            return;
        }

        if (TryGlue(uid, component, target))
        {
            args.Handled = true;
            _audio.PlayPvs(component.Squeeze, uid);
            _popup.PopupEntity(Loc.GetString("glue-success", ("target", Identity.Entity(target, EntityManager))), args.User, args.User, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("glue-failure", ("target", Identity.Entity(target, EntityManager))), args.User, args.User, PopupType.Medium);
        }
    }

    private bool TryGlue(EntityUid uid, GlueComponent component, EntityUid target)
    {
        if (HasComp<GluedComponent>(target) || !HasComp<ItemComponent>(target))
        {
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            var quantity = solution.RemoveReagent(component.Reagent, component.Consumption);
            if (quantity > 0)
            {
                EnsureComp<GluedComponent>(target).Duration = quantity.Double() * component.Duration;
                return true;
            }
        }
        return false;
    }
}
