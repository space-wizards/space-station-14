using Content.Server.Revolutionary.Components;
using Content.Shared.Revolutionary.Components;
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
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.GameTicking;
using Content.Server.GameTicking;
using Robust.Shared.Timing;
using System.Threading;

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
        SubscribeLocalEvent<USSPUplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        
        // Subscribe to the update event to periodically synchronize uplinks
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
    }
    
    private CancellationTokenSource? _syncTimerCancelToken;
    
    private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New == GameRunLevel.InRound)
        {
            // Start a timer to periodically synchronize uplinks
            _syncTimerCancelToken = new CancellationTokenSource();
            Robust.Shared.Timing.Timer.SpawnRepeating(5000, () => SynchronizeAllUplinks(), _syncTimerCancelToken.Token);
        }
    }
    
    /// <summary>
    /// Synchronizes all uplinks in the game to ensure they have the correct currency values.
    /// This is called periodically to ensure that uplinks are always in sync.
    /// </summary>
    public void SynchronizeAllUplinks()
    {
        Logger.InfoS("ussp-uplink", "Synchronizing all uplinks in the game");
        
        // Get all head revolutionaries
        var headRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
        foreach (var headRev in headRevs)
        {
            // Get the revolutionary rule system
            var revRuleSystem = EntitySystem.Get<Content.Server.GameTicking.Rules.RevolutionaryRuleSystem>();
            
            // Call the SynchronizeAllUplinksByOwner method for each head revolutionary
            revRuleSystem.SynchronizeAllUplinksByOwner(headRev.Owner);
        }
    }
    
    /// <summary>
    /// Directly synchronizes telebonds and conversions between two uplinks.
    /// This ensures that when a revolutionary gets a head revolutionary's uplink,
    /// they immediately receive the correct currency values.
    /// </summary>
    public void SyncUplinkCurrencies(EntityUid sourceUplinkUid, EntityUid targetUplinkUid)
    {
        if (!TryComp<StoreComponent>(sourceUplinkUid, out var sourceStore) || 
            !TryComp<StoreComponent>(targetUplinkUid, out var targetStore))
            return;
            
        // Get the currency values from the source uplink
        var sourceTelebond = sourceStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
        var sourceConversion = sourceStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
        
        // Make sure the target store has both currencies initialized
        if (!targetStore.Balance.ContainsKey("Telebond"))
        {
            targetStore.Balance["Telebond"] = FixedPoint2.Zero;
        }
        
        if (!targetStore.Balance.ContainsKey("Conversion"))
        {
            targetStore.Balance["Conversion"] = FixedPoint2.Zero;
        }
        
        // Update the target uplink with the source values if they're higher
        if (targetStore.Balance["Telebond"] < sourceTelebond)
        {
            targetStore.Balance["Telebond"] = sourceTelebond;
            Logger.InfoS("ussp-uplink", $"Updated Telebond currency in uplink {ToPrettyString(targetUplinkUid)} to {sourceTelebond}");
        }
        
        if (targetStore.Balance["Conversion"] < sourceConversion)
        {
            targetStore.Balance["Conversion"] = sourceConversion;
            Logger.InfoS("ussp-uplink", $"Updated Conversion currency in uplink {ToPrettyString(targetUplinkUid)} to {sourceConversion}");
        }
    }
        
        /// <summary>
        /// Handles the event when a USSP uplink implant is implanted.
        /// If the implanted entity is a head revolutionary, updates their HeadRevolutionaryImplantComponent
        /// to point to this implant.
        /// </summary>
        private void OnImplantImplanted(EntityUid uid, USSPUplinkImplantComponent component, ref ImplantImplantedEvent args)
        {
            if (args.Implanted == null)
                return;
            
            // First, check if this uplink already has an owner
            EntityUid? originalOwner = null;
            if (TryComp<USSPUplinkOwnerComponent>(uid, out var existingOwnerComp) && existingOwnerComp.OwnerUid != null)
            {
                originalOwner = existingOwnerComp.OwnerUid;
                Logger.InfoS("ussp-uplink", $"Uplink {ToPrettyString(uid)} already has an owner: {ToPrettyString(originalOwner.Value)}");
            }
                
            // Check if the implanted entity is a head revolutionary
            if (HasComp<HeadRevolutionaryComponent>(args.Implanted.Value))
            {
                Logger.InfoS("ussp-uplink", $"USSP uplink implant {ToPrettyString(uid)} implanted in head revolutionary {ToPrettyString(args.Implanted.Value)}");
                
                // Update the head revolutionary's implant component to point to this implant
                var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted.Value);
                implantComponent.ImplantUid = uid;
                
                // If the uplink doesn't have an owner yet, set this head revolutionary as the owner
                if (originalOwner == null)
                {
                    var uplinkOwnerComp = EnsureComp<USSPUplinkOwnerComponent>(uid);
                    uplinkOwnerComp.OwnerUid = args.Implanted.Value;
                    
                    Logger.InfoS("ussp-uplink", $"Added USSPUplinkOwnerComponent to uplink {ToPrettyString(uid)} with owner {ToPrettyString(args.Implanted.Value)}");
                }
                
                // Show the current telebond and conversion values
                if (TryComp<StoreComponent>(uid, out var store))
                {
                    var telebonds = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    var conversions = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                    
                    _popup.PopupEntity(Loc.GetString($"Uplink implanted. Current Telebonds: {telebonds}, Conversions: {conversions}"), 
                        args.Implanted.Value, args.Implanted.Value, PopupType.Medium);
                }
            }
            // Check if the implanted entity is a regular revolutionary
            else if (HasComp<RevolutionaryComponent>(args.Implanted.Value))
            {
                Logger.InfoS("ussp-uplink", $"USSP uplink implant {ToPrettyString(uid)} implanted in revolutionary {ToPrettyString(args.Implanted.Value)}");
                
                // If the uplink doesn't have an owner component yet, try to find its owner
                if (originalOwner == null)
                {
                    var ownerComp = EnsureComp<USSPUplinkOwnerComponent>(uid);
                    Logger.InfoS("ussp-uplink", $"Added missing USSPUplinkOwnerComponent to uplink {ToPrettyString(uid)}");
                    
                    // Try to find a head revolutionary who might own this uplink
                    var headRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                    foreach (var (_, headRevImplant) in headRevs)
                    {
                        if (headRevImplant.ImplantUid == uid)
                        {
                            ownerComp.OwnerUid = headRevImplant.Owner;
                            Logger.InfoS("ussp-uplink", $"Found owner for uplink: {ToPrettyString(headRevImplant.Owner)}");
                            originalOwner = headRevImplant.Owner;
                            break;
                        }
                    }
                    
                    // If we still don't have an owner, try to find a head revolutionary who has this implant
                    if (ownerComp.OwnerUid == null)
                    {
                        // Get all head revolutionaries
                        var allHeadRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
                        foreach (var headRev in allHeadRevs)
                        {
                            // Check if this head revolutionary has this implant
                            var implantSystem = EntitySystem.Get<SubdermalImplantSystem>();
                            if (implantSystem.TryGetImplants(headRev.Owner, out var implants))
                            {
                                foreach (var implant in implants)
                                {
                                    if (implant == uid)
                                    {
                                        ownerComp.OwnerUid = headRev.Owner;
                                        originalOwner = headRev.Owner;
                                        Logger.InfoS("ussp-uplink", $"Found owner for uplink by checking implants: {ToPrettyString(headRev.Owner)}");
                                        
                                        // Also update the head revolutionary's implant component
                                        var headRevImplantComp = EnsureComp<HeadRevolutionaryImplantComponent>(headRev.Owner);
                                        headRevImplantComp.ImplantUid = uid;
                                        Logger.InfoS("ussp-uplink", $"Updated head revolutionary implant component to point to {ToPrettyString(uid)}");
                                        
                                        break;
                                    }
                                }
                                
                                if (ownerComp.OwnerUid != null)
                                    break;
                            }
                        }
                    }
                }
                
                // Add HeadRevolutionaryImplantComponent to the revolutionary to track the implant
                var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted.Value);
                implantComponent.ImplantUid = uid;
                
                Logger.InfoS("ussp-uplink", $"Added HeadRevolutionaryImplantComponent to revolutionary {ToPrettyString(args.Implanted.Value)}");
                
                // Ensure the store component has the correct currencies
                if (TryComp<StoreComponent>(uid, out var store))
                {
                    // Make sure the store has both currencies initialized
                    if (!store.Balance.ContainsKey("Telebond"))
                    {
                        store.Balance["Telebond"] = FixedPoint2.Zero;
                    }
                    
                    if (!store.Balance.ContainsKey("Conversion"))
                    {
                        store.Balance["Conversion"] = FixedPoint2.Zero;
                    }
                    
                    // Find all uplinks owned by head revolutionaries and get the maximum currency values
                    // This ensures we get the correct values even if the original owner isn't properly set
                    var allUplinks = new List<EntityUid>();
                    var maxTelebond = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    var maxConversion = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                    
                    // First, find all head revolutionaries
                    var headRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
                    foreach (var headRev in headRevs)
                    {
                        Logger.InfoS("ussp-uplink", $"Checking head revolutionary {ToPrettyString(headRev.Owner)} for uplinks");
                        
                        // Check if this head revolutionary has an implant component
                        if (TryComp<HeadRevolutionaryImplantComponent>(headRev.Owner, out var headRevImplant) && 
                            headRevImplant.ImplantUid != null && 
                            EntityManager.EntityExists(headRevImplant.ImplantUid.Value))
                        {
                            var headRevUplinkUid = headRevImplant.ImplantUid.Value;
                            allUplinks.Add(headRevUplinkUid);
                            
                            // Check if this uplink has a store component
                            if (TryComp<StoreComponent>(headRevUplinkUid, out var headRevStore))
                            {
                                // Get the currency values
                                var telebonds = headRevStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                                var conversions = headRevStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                
                                Logger.InfoS("ussp-uplink", $"Found uplink {ToPrettyString(headRevUplinkUid)} with Telebond: {telebonds}, Conversion: {conversions}");
                                
                                // Update the maximum values
                                if (telebonds > maxTelebond)
                                {
                                    maxTelebond = telebonds;
                                    Logger.InfoS("ussp-uplink", $"Found higher Telebond value: {telebonds}");
                                }
                                
                                if (conversions > maxConversion)
                                {
                                    maxConversion = conversions;
                                    Logger.InfoS("ussp-uplink", $"Found higher Conversion value: {conversions}");
                                }
                            }
                        }
                    }
                    
                    // Also check all uplinks with USSPUplinkOwnerComponent
                    var uplinkQuery = EntityManager.EntityQuery<USSPUplinkOwnerComponent, StoreComponent>();
                    foreach (var (uplinkOwner, uplinkStore) in uplinkQuery)
                    {
                        if (!allUplinks.Contains(uplinkOwner.Owner))
                        {
                            allUplinks.Add(uplinkOwner.Owner);
                            
                            // Get the currency values
                            var otherTelebonds = uplinkStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                            var otherConversions = uplinkStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                            
                            Logger.InfoS("ussp-uplink", $"Found uplink {ToPrettyString(uplinkOwner.Owner)} with Telebond: {otherTelebonds}, Conversion: {otherConversions}");
                            
                            // Update the maximum values
                            if (otherTelebonds > maxTelebond)
                            {
                                maxTelebond = otherTelebonds;
                                Logger.InfoS("ussp-uplink", $"Found higher Telebond value: {otherTelebonds}");
                            }
                            
                            if (otherConversions > maxConversion)
                            {
                                maxConversion = otherConversions;
                                Logger.InfoS("ussp-uplink", $"Found higher Conversion value: {otherConversions}");
                            }
                        }
                    }
                    
                    // Update the current uplink with the maximum values
                    if (maxTelebond > store.Balance["Telebond"])
                    {
                        store.Balance["Telebond"] = maxTelebond;
                        Logger.InfoS("ussp-uplink", $"Updated current uplink Telebond to {maxTelebond}");
                    }
                    
                    if (maxConversion > store.Balance["Conversion"])
                    {
                        store.Balance["Conversion"] = maxConversion;
                        Logger.InfoS("ussp-uplink", $"Updated current uplink Conversion to {maxConversion}");
                    }
                        
                    // Now update all other uplinks with the maximum values
                    foreach (var otherUplink in allUplinks)
                    {
                        if (otherUplink == uid)
                            continue;
                            
                        if (TryComp<StoreComponent>(otherUplink, out var otherStore))
                        {
                            // Make sure the store has both currencies initialized
                            if (!otherStore.Balance.ContainsKey("Telebond"))
                            {
                                otherStore.Balance["Telebond"] = FixedPoint2.Zero;
                            }
                            
                            if (!otherStore.Balance.ContainsKey("Conversion"))
                            {
                                otherStore.Balance["Conversion"] = FixedPoint2.Zero;
                            }
                            
                            // Update the currencies if they're lower than the maximum
                            if (otherStore.Balance["Telebond"] < maxTelebond)
                            {
                                otherStore.Balance["Telebond"] = maxTelebond;
                                Logger.InfoS("ussp-uplink", $"Updated Telebond currency in uplink {ToPrettyString(otherUplink)} to {maxTelebond}");
                            }
                            
                            if (otherStore.Balance["Conversion"] < maxConversion)
                            {
                                otherStore.Balance["Conversion"] = maxConversion;
                                Logger.InfoS("ussp-uplink", $"Updated Conversion currency in uplink {ToPrettyString(otherUplink)} to {maxConversion}");
                            }
                        }
                    }
                    
                    var finalTelebonds = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    var finalConversions = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                    
                    var ownerInfo = originalOwner != null ? $" (owned by {Identity.Name(originalOwner.Value, EntityManager)})" : "";
                    _popup.PopupEntity(Loc.GetString($"Uplink implanted{ownerInfo}. Current Telebonds: {finalTelebonds}, Conversions: {finalConversions}"), 
                        args.Implanted.Value, args.Implanted.Value, PopupType.Medium);
                }
                
                // If we have an original owner, notify them that their uplink has been implanted in someone else
                if (originalOwner != null && originalOwner.Value != args.Implanted.Value)
                {
                    _popup.PopupEntity(Loc.GetString($"Your uplink has been implanted in {Identity.Name(args.Implanted.Value, EntityManager)}"), 
                        originalOwner.Value, originalOwner.Value, PopupType.Medium);
                }
            }
        }
        
        /// <summary>
        /// Adds Conversion currency to all head revolutionary uplinks.
        /// This is a shared counter that tracks total conversions by all head revolutionaries.
        /// </summary>
        public void AddConversionToAllHeadRevs(StoreSystem storeSystem)
        {
            Logger.InfoS("ussp-uplink", "Adding Conversion to all head revolutionary uplinks");
            
            // Get all USSPUplinkImplant entities in the game
            var query = EntityManager.EntityQuery<MetaDataComponent, StoreComponent>(true);
            var uplinkEntities = new List<EntityUid>();
            
            foreach (var (metadata, _) in query)
            {
                if (metadata.EntityPrototype?.ID == "USSPUplinkImplant")
                {
                    uplinkEntities.Add(metadata.Owner);
                }
            }
            
            // If no uplinks were found, log a warning
            if (uplinkEntities.Count == 0)
            {
                Logger.WarningS("ussp-uplink", "No USSP uplink implants found in the game");
                return;
            }
            
            // Add Conversion to all uplinks
            foreach (var uplinkEntity in uplinkEntities)
            {
                var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } };
                var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkEntity);
                Logger.InfoS("ussp-uplink", $"Added Conversion to uplink {ToPrettyString(uplinkEntity)}, success: {success}");
            }
            
            // Show popup to all head revolutionaries (private)
            var headRevQuery = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
            foreach (var headRev in headRevQuery)
            {
                _popup.PopupEntity(Loc.GetString("+1 Conversion"), headRev.Owner, headRev.Owner, PopupType.Medium);
            }
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
                //if (TryComp<SubdermalImplantComponent>(args.StoreUid, out var implant) && implant.ImplantedEntity != null)
                //{
                //    _popup.PopupEntity("Conversion is not spent", implant.ImplantedEntity.Value, PopupType.Medium);
                //}
                //
                //Logger.InfoS("ussp-uplink", $"Restored {spentAmount} Conversion currency after purchase");
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
