using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Timing;
using Content.Shared.Cargo.Systems;

namespace Content.Server.Cargo.Systems;

public sealed class PriceGunSystem : SharedPriceGunSystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly CargoSystem _bountySystem = default!;

    protected override bool GetPriceOrBounty(EntityUid priceGunUid, EntityUid target, EntityUid user)
    {
        if (!TryComp(priceGunUid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((priceGunUid, useDelay)))
            return false;

        // Check if we're scanning a bounty crate
        if (_bountySystem.IsBountyComplete(target, out _))
        {
            _popupSystem.PopupEntity(Loc.GetString("price-gun-bounty-complete"), user, user);
        }
        else // Otherwise appraise the price
        {
            var price = _pricingSystem.GetPrice(target);
            _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", Identity.Entity(target, EntityManager)), ("price", $"{price:F2}")), user, user);
        }

        _useDelay.TryResetDelay((priceGunUid, useDelay));
        return true;
    }
}
