using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Timing;

namespace Content.Server.CartridgeLoader.Cartridges
{
    public sealed class SpaceVendorsCartridgeSystem : EntitySystem
    {
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly PricingSystem _pricingSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpaceVendorsCartridgeComponent, CartridgeAfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, SpaceVendorsCartridgeComponent component, CartridgeAfterInteractEvent args)
        {
            if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
                return;

            var price = _pricingSystem.GetPrice(args.InteractEvent.Target.Value);

            _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result",
                ("object", Identity.Entity(args.InteractEvent.Target.Value, EntityManager)),
                ("price", $"{price:F2}")), args.InteractEvent.User, args.InteractEvent.User);
            args.InteractEvent.Handled = true;
        }
    }
}
