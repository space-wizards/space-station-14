using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Throwing;

namespace Content.Server.CombatMode.Pacification;

public sealed class PacificationSystem : SharedPacificationSystem
{
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly OpenableSystem _openable = default!;
    [Dependency] private   readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifiedComponent, BeforeThrowEvent>(OnBeforeThrow);
    }

    private void OnBeforeThrow(EntityUid playerUid, PacifiedComponent component, BeforeThrowEvent args)
    {
        var thrownItem = args.ItemUid;
        var itemName = Identity.Entity(thrownItem, EntityManager);
        // Check whether the item being thrown is excluded by the blacklist:
        if (component.ThrowBlacklist.IsValid(thrownItem, EntityManager))
        {
            args.Handled = true;
            // Tell the player why they can’t throw stuff:
            var cannotThrowMessage = "pacified-cannot-throw";
            if (HasComp<SlipperyComponent>(thrownItem))
                cannotThrowMessage = "pacified-cannot-throw-slippery";
            _popup.PopupEntity(Loc.GetString(cannotThrowMessage, ("projectile", itemName)), playerUid, playerUid);
        }
        // Or whether it can be spilled easily and has something to spill.
        else if (HasComp<SpillableComponent>(thrownItem)
                 && !_openable.IsClosed(thrownItem)
                 && _solutionContainerSystem.PercentFull(thrownItem) > 0)
        {
            // Further, check that the item does not contain harmful reagents:
            /* TODO: I guess this should call out to ReactiveSystem, but there is no method that checks whether a
                     reaction *can* take place. As it stands, anything that can spill is forbidden. */
            // DoACheck();
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("pacified-cannot-throw-spill", ("projectile", itemName)), playerUid, playerUid);
        }
    }
}
