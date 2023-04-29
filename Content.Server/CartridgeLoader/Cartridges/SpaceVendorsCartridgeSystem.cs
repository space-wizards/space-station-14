using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.IdentityManagement;

namespace Content.Server.CartridgeLoader.Cartridges
{
    public sealed class SpaceVendorsCartridgeSystem : EntitySystem
    {
        [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
        [Dependency] private readonly PricingSystem _pricingSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpaceVendorsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
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

            //NetProbeCartridgeSystem: Limit the amount of saved probe results to 9
            //This is hardcoded because the UI doesn't support a dynamic number of results
            if (component.AppraisedItems.Count >= component.MaxSavedItems)
                component.AppraisedItems.RemoveAt(0);

            var item = new AppraisedItem(
                Name(args.InteractEvent.Target.Value),
                $"{price:F2}",
                _gameTicker.RoundDuration().Minutes);

            component.AppraisedItems.Add(item);
            UpdateUiState(uid,args.Loader,component);
        }

        private void OnUiReady(EntityUid uid, SpaceVendorsCartridgeComponent component, CartridgeUiReadyEvent args)
        {
            UpdateUiState(uid, args.Loader, component);
        }

        private void UpdateUiState(EntityUid uid, EntityUid loaderUid, SpaceVendorsCartridgeComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;
            UpdateElapsedTimeData(component);
            var state = new SpaceVendorsUiState(component.AppraisedItems);
            _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
        }

        private void UpdateElapsedTimeData(SpaceVendorsCartridgeComponent component)
        {
            foreach (var item in component.AppraisedItems)
            {
                item.Minutes = _gameTicker.RoundDuration().Minutes - item.MinutesCreation;
            }
        }
    }
}
