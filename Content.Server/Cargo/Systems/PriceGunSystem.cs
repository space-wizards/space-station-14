using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class PriceGunSystem : EntitySystem
{
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PriceGunComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, PriceGunComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        var price = _pricingSystem.GetPrice(args.Target.Value);

        _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", args.Target.Value), ("price", $"{price:F2}")), args.User, Filter.Entities(args.User));
    }
}
