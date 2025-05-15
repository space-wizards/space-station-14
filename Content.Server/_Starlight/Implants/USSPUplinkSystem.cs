using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Content.Shared.FixedPoint;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server.Implants
{
    public sealed class USSPUplinkSystem : EntitySystem
    {
        [Dependency] private readonly StoreSystem _storeSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<Content.Shared.Actions.OpenUplinkImplantEvent>(OnOpenUplinkImplant);
            SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
        }
        
        /// <summary>
        /// Handles the event when a purchase is made from a store.
        /// Restores any spent Conversion currency since it's a global headrev progression score, not a spendable currency.
        /// </summary>
        private void OnStoreBuyFinished(ref StoreBuyFinishedEvent args)
        {
            // Get the store component
            if (!_entityManager.TryGetComponent(args.StoreUid, out StoreComponent? store))
                return;
            
            // Check if this store uses Conversion currency
            if (!store.CurrencyWhitelist.Contains("Conversion"))
                return;
            
            // Check if Conversion was spent in this purchase
            bool conversionWasSpent = false;
            foreach (var (currency, amount) in args.PurchasedItem.Cost)
            {
                if (currency == "Conversion" && amount > FixedPoint2.Zero)
                {
                    conversionWasSpent = true;
                    break;
                }
            }
            
            if (conversionWasSpent)
            {
                // Calculate how much Conversion was spent
                FixedPoint2 spentAmount = FixedPoint2.Zero;
                foreach (var (currency, amount) in args.PurchasedItem.Cost)
                {
                    if (currency == "Conversion")
                    {
                        spentAmount = amount;
                        break;
                    }
                }
                
                // Add the Conversion currency back
                var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Conversion", spentAmount } };
                _storeSystem.TryAddCurrency(currencyToAdd, args.StoreUid);
                
                // Find the owner of the uplink
                if (TryComp<SubdermalImplantComponent>(args.StoreUid, out var implant) && implant.ImplantedEntity != null)
                {
                    _popup.PopupEntity("Conversion is not spent", implant.ImplantedEntity.Value, PopupType.Medium);
                }
                
                Logger.InfoS("ussp-uplink", $"Restored {spentAmount} Conversion currency after purchase");
            }
        }

        private void OnOpenUplinkImplant(Content.Shared.Actions.OpenUplinkImplantEvent args)
        {
            var user = args.User;
            if (!_entityManager.TryGetComponent(user, out StoreComponent? store))
                return;

            // Check if the user has the USSP uplink implant store
            if (!store.Balance.ContainsKey("Telebond"))
                return;

            // Open the USSP uplink UI (StoreBoundUserInterface)
            _storeSystem.ToggleUi(user, store.Owner, store);
            Logger.DebugS("ussp-uplink", $"Opened USSP uplink UI for {ToPrettyString(user)}");
        }
    }
}
