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
using Content.Server.RoundEnd;
using Robust.Shared.Timing;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Implants
{
    public sealed class USSPUplinkSystem : EntitySystem
    {
        [Dependency] private readonly StoreSystem _storeSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Content.Shared.Actions.OpenUplinkImplantEvent>(OnOpenUplinkImplant);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
        SubscribeLocalEvent<USSPUplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEnd);
    }
    
    /// <summary>
    /// Handles the event when a round ends.
    /// Resets all stock-limited listings in the uplink catalog.
    /// </summary>
    private void OnRoundEnd(RoundEndSystemChangedEvent ev)
    {
        // Reset all stock-limited listings in the uplink catalog
        ResetUplinkStocks();
    }
    
    /// <summary>
    /// Resets all stock-limited listings in the uplink catalog.
    /// This is called when a round ends to ensure each new round starts with fresh stock counts.
    /// </summary>
    private void ResetUplinkStocks()
    {
        // Use reflection to access the private static dictionaries in StockLimitedListingCondition
        var type = typeof(Content.Shared.Store.Conditions.StockLimitedListingCondition);
        
        // Get the _stockCounts dictionary
        var stockCountsField = type.GetField("_stockCounts", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Get the _stockLimits dictionary
        var stockLimitsField = type.GetField("_stockLimits", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Get the _lastPurchasers dictionary
        var lastPurchasersField = type.GetField("_lastPurchasers", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Get the _outOfStock dictionary
        var outOfStockField = type.GetField("_outOfStock", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Get the _modifiedListings dictionary
        var modifiedListingsField = type.GetField("_modifiedListings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Reset the dictionaries
        if (stockCountsField != null)
        {
            var stockCounts = stockCountsField.GetValue(null) as Dictionary<string, int>;
            if (stockCounts != null)
            {
                stockCounts.Clear();
            }
        }
        
        if (stockLimitsField != null)
        {
            var stockLimits = stockLimitsField.GetValue(null) as Dictionary<string, int>;
            if (stockLimits != null)
            {
                stockLimits.Clear();
            }
        }
        
        if (lastPurchasersField != null)
        {
            var lastPurchasers = lastPurchasersField.GetValue(null) as Dictionary<string, string>;
            if (lastPurchasers != null)
            {
                lastPurchasers.Clear();
            }
        }
        
        if (outOfStockField != null)
        {
            var outOfStock = outOfStockField.GetValue(null) as Dictionary<string, bool>;
            if (outOfStock != null)
            {
                outOfStock.Clear();
            }
        }
        
        if (modifiedListingsField != null)
        {
            var modifiedListings = modifiedListingsField.GetValue(null) as Dictionary<string, bool>;
            if (modifiedListings != null)
            {
                modifiedListings.Clear();
            }
        }
        
        // Update all uplink UIs to show the reset stock counts
        UpdateAllUplinkListings();
    }
    
    
    /// <summary>
    /// Synchronizes all uplinks in the game to ensure they have the correct currency values.
    /// This is called periodically to ensure that uplinks are always in sync.
    /// </summary>
    public void SynchronizeAllUplinks()
    {        
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
    /// 
    /// For telebonds, it only synchronizes if both uplinks have the same owner.
    /// For conversions, it always synchronizes as this is a global counter.
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
        
        // Check if both uplinks have the same owner for telebond synchronization
        bool sameOwner = false;
        EntityUid? sourceOwner = null;
        EntityUid? targetOwner = null;
        
        if (TryComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(sourceUplinkUid, out var sourceOwnerComp))
            sourceOwner = sourceOwnerComp.OwnerUid;
            
        if (TryComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(targetUplinkUid, out var targetOwnerComp))
            targetOwner = targetOwnerComp.OwnerUid;
            
        // If both uplinks have the same owner, or if one doesn't have an owner but the other does
        if (sourceOwner != null && targetOwner != null && sourceOwner == targetOwner)
        {
            sameOwner = true;
        }
        // If target doesn't have an owner but source does, set target's owner to match source
        else if (sourceOwner != null && targetOwner == null)
        {
            var newOwnerComp = EnsureComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(targetUplinkUid);
            newOwnerComp.OwnerUid = sourceOwner;
            sameOwner = true;
        }
        // If source doesn't have an owner but target does, set source's owner to match target
        else if (sourceOwner == null && targetOwner != null)
        {
            var newOwnerComp = EnsureComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(sourceUplinkUid);
            newOwnerComp.OwnerUid = targetOwner;
            sameOwner = true;
        }
        
        // Update the target uplink with the source telebond value if they're higher AND they have the same owner
        if (sameOwner && targetStore.Balance["Telebond"] < sourceTelebond)
        {
            targetStore.Balance["Telebond"] = sourceTelebond;
        }
        
        // Always update Conversion as it's a global counter
        if (targetStore.Balance["Conversion"] < sourceConversion)
        {
            targetStore.Balance["Conversion"] = sourceConversion;
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
                if (TryComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(uid, out var existingOwnerComp) && existingOwnerComp.OwnerUid != null)
            {
                originalOwner = existingOwnerComp.OwnerUid;
            }
                
                // Check if the implanted entity is a head revolutionary
            if (HasComp<HeadRevolutionaryComponent>(args.Implanted))
            {                
                // Update the head revolutionary's implant component to point to this implant
                var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted);
                implantComponent.ImplantUid = uid;
                
                // If the uplink doesn't have an owner yet, set this head revolutionary as the owner
                if (originalOwner == null)
                {
                    var uplinkOwnerComp = EnsureComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(uid);
                    uplinkOwnerComp.OwnerUid = args.Implanted;
                    originalOwner = args.Implanted;
                }
                
                // Ensure the store component has both currencies initialized
                if (TryComp<StoreComponent>(uid, out var store))
                {
                    if (!store.Balance.ContainsKey("Telebond"))
                    {
                        store.Balance["Telebond"] = FixedPoint2.Zero;
                    }
                    
                    if (!store.Balance.ContainsKey("Conversion"))
                    {
                        store.Balance["Conversion"] = FixedPoint2.Zero;
                    }
                }
                
                // Find all revolutionaries that were converted by this head revolutionary
                // and add telebonds for each one
                var convertedRevs = EntityManager.EntityQuery<RevolutionaryComponent, RevolutionaryConverterComponent>();
                int convertedCount = 0;
                
                foreach (var (_, converterComp) in convertedRevs)
                {
                    // Check if this revolutionary was converted by this head revolutionary
                    if (converterComp.ConverterUid == args.Implanted)
                    {
                        convertedCount++;
                        
                        // Add a telebond for each converted revolutionary
                        if (TryComp<StoreComponent>(uid, out var storeComp))
                        {
                            // Make sure the store has the Telebond currency initialized
                            if (!storeComp.Balance.ContainsKey("Telebond"))
                            {
                                storeComp.Balance["Telebond"] = FixedPoint2.Zero;
                            }
                            
                            // Add a telebond
                            storeComp.Balance["Telebond"] += FixedPoint2.New(1);
                        }
                    }
                }
                
                // Synchronize all uplinks to ensure this one has the correct values
                SynchronizeAllUplinks();
                
                // Show the current telebond and conversion values
                if (TryComp<StoreComponent>(uid, out var storeAfterSync))
                {
                    var telebonds = storeAfterSync.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    var conversions = storeAfterSync.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                    
                    var convertedMessage = convertedCount > 0 ? $" (+{convertedCount} from previous conversions)" : "";
                    _popup.PopupEntity(Loc.GetString($"Implanted! Current Telebonds: {telebonds}{convertedMessage}, Conversions: {conversions}"), 
                        args.Implanted, args.Implanted, PopupType.Medium);
                }
                
                // If the head revolutionary is implanting an uplink that belongs to another head revolutionary,
                // we need to update the ownership to the current head revolutionary
                if (originalOwner != null && originalOwner.Value != args.Implanted)
                {
                    // Only change ownership if the implanted entity is a head revolutionary
                    var uplinkOwnerComp = EnsureComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(uid);
                    uplinkOwnerComp.OwnerUid = args.Implanted;
                                        
                    // Notify the original owner that their uplink has been claimed by another head revolutionary
                    _popup.PopupEntity(Loc.GetString($"Your uplink has been claimed by {Identity.Name(args.Implanted, EntityManager)}"), 
                        originalOwner.Value, originalOwner.Value, PopupType.Medium);
                }
            }
            // Check if the implanted entity is a regular revolutionary
            else if (HasComp<RevolutionaryComponent>(args.Implanted))
            {                
                // If the uplink doesn't have an owner component yet, try to find its owner
                if (originalOwner == null)
                {
                    var ownerComp = EnsureComp<Content.Shared.Implants.Components.USSPUplinkOwnerComponent>(uid);                    
                    // Try to find a head revolutionary who might own this uplink
                    var headRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                    foreach (var (_, headRevImplant) in headRevs)
                    {
                        if (headRevImplant.ImplantUid == uid)
                        {
                            ownerComp.OwnerUid = headRevImplant.Owner;
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
                                        
                                        // Also update the head revolutionary's implant component
                                        var headRevImplantComp = EnsureComp<HeadRevolutionaryImplantComponent>(headRev.Owner);
                                        headRevImplantComp.ImplantUid = uid;                                        
                                        break;
                                    }
                                }
                                
                                if (ownerComp.OwnerUid != null)
                                    break;
                            }
                        }
                    }
                    
                    // If we still don't have an owner, find the head revolutionary who most recently converted this revolutionary
                    if (ownerComp.OwnerUid == null)
                    {
                        // Get all head revolutionaries
                        var allHeadRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                        foreach (var (_, headRevImplant) in allHeadRevs)
                        {
                            if (headRevImplant.ImplantUid != null && EntityManager.EntityExists(headRevImplant.ImplantUid.Value))
                            {
                                // Set this head revolutionary as the owner of the uplink
                                ownerComp.OwnerUid = headRevImplant.Owner;
                                originalOwner = headRevImplant.Owner;
                                
                                // Directly sync with this head revolutionary's uplink
                                SyncUplinkCurrencies(headRevImplant.ImplantUid.Value, uid);
                                break;
                            }
                        }
                    }
                }
                
                // Add HeadRevolutionaryImplantComponent to the revolutionary to track the implant
                var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted);
                implantComponent.ImplantUid = uid;
                                
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
                    
                    // Find all uplinks owned by the same head revolutionary and get the maximum currency values
                    var allUplinks = new List<EntityUid>();
                    var maxTelebond = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    var maxConversion = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                    
                    // Only synchronize telebonds with uplinks from the same owner
                    if (originalOwner != null)
                    {
                        // Find all uplinks owned by this head revolutionary
                        var uplinkQuery = EntityManager.EntityQuery<Content.Shared.Implants.Components.USSPUplinkOwnerComponent, StoreComponent>();
                        foreach (var (uplinkOwner, uplinkStore) in uplinkQuery)
                        {
                            if (uplinkOwner.OwnerUid == originalOwner && uplinkOwner.Owner != uid)
                            {
                                allUplinks.Add(uplinkOwner.Owner);
                                
                                // Get the currency values
                                var otherTelebonds = uplinkStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                                var otherConversions = uplinkStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                                                
                                // Update the maximum values
                                if (otherTelebonds > maxTelebond)
                                {
                                    maxTelebond = otherTelebonds;
                                }
                                
                                if (otherConversions > maxConversion)
                                {
                                    maxConversion = otherConversions;
                                }
                            }
                        }
                        
                        // Also check if the owner has an uplink
                        if (TryComp<HeadRevolutionaryImplantComponent>(originalOwner.Value, out var ownerImplant) && 
                            ownerImplant.ImplantUid != null && 
                            EntityManager.EntityExists(ownerImplant.ImplantUid.Value) &&
                            ownerImplant.ImplantUid.Value != uid)
                        {
                            var ownerUplinkUid = ownerImplant.ImplantUid.Value;
                            
                            if (TryComp<StoreComponent>(ownerUplinkUid, out var ownerStore))
                            {
                                var ownerTelebonds = ownerStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                                var ownerConversions = ownerStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                                                
                                if (ownerTelebonds > maxTelebond)
                                {
                                    maxTelebond = ownerTelebonds;
                                }
                                
                                if (ownerConversions > maxConversion)
                                {
                                    maxConversion = ownerConversions;
                                }
                            }
                        }
                    }
                    else
                    {
                        // If we don't have an owner, just get the global maximum conversion value
                        // This ensures we get the correct values even if the original owner isn't properly set
                        var uplinkQuery = EntityManager.EntityQuery<StoreComponent>();
                        foreach (var uplinkStore in uplinkQuery)
                        {
                            if (uplinkStore.Owner == uid)
                                continue;
                                
                            if (uplinkStore.Balance.TryGetValue("Conversion", out var conversion) && conversion > maxConversion)
                            {
                                maxConversion = conversion;
                            }
                        }
                    }
                    
                    // Update the current uplink with the maximum values
                    if (maxTelebond > store.Balance["Telebond"])
                    {
                        store.Balance["Telebond"] = maxTelebond;
                    }
                    
                    if (maxConversion > store.Balance["Conversion"])
                    {
                        store.Balance["Conversion"] = maxConversion;
                    }
                    
                    // Synchronize all uplinks to ensure this one has the correct values
                    SynchronizeAllUplinks();
                    
                    // Always show +1 telebond and +1 conversion in the popup, regardless of the actual values
                    //var ownerInfo = originalOwner != null ? $" (owned by {Identity.Name(originalOwner.Value, EntityManager)})" : "";
                    //_popup.PopupEntity(Loc.GetString($"Uplink implanted{ownerInfo}. +1 Telebond, +1 Conversion"), 
                    //    args.Implanted, args.Implanted, PopupType.Medium);
                }
                
                // If we have an original owner, notify them that their uplink has been implanted in someone else
                if (originalOwner != null && originalOwner.Value != args.Implanted)
                {
                    _popup.PopupEntity(Loc.GetString($"Your uplink has been implanted in {Identity.Name(args.Implanted, EntityManager)}"), 
                        originalOwner.Value, originalOwner.Value, PopupType.Medium);
                    
                    // Call the RevolutionaryRuleSystem to synchronize all uplinks owned by this head revolutionary
                    var revRuleSystem = EntitySystem.Get<Content.Server.GameTicking.Rules.RevolutionaryRuleSystem>();
                    revRuleSystem.SynchronizeAllUplinksByOwner(originalOwner.Value);
                    
                    // Special case: If the implanted entity was just converted by the head revolutionary,
                    // we need to make sure the uplink has the latest values
                    if (HasComp<RevolutionaryComponent>(args.Implanted) && 
                        TryComp<HeadRevolutionaryImplantComponent>(originalOwner.Value, out var headRevImplant) && 
                        headRevImplant.ImplantUid != null && 
                        EntityManager.EntityExists(headRevImplant.ImplantUid.Value))
                    {
                        // Directly sync the currencies from the head revolutionary's uplink to this uplink
                        SyncUplinkCurrencies(headRevImplant.ImplantUid.Value, uid);
                        
                        // Update the UI to show the latest values
                        if (TryComp<StoreComponent>(uid, out var finalStore))
                        {
                            var finalTelebonds = finalStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                            var finalConversions = finalStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                            
                            // Show an additional popup with the updated values
                            _popup.PopupEntity(Loc.GetString($"Uplink synchronized! Current Telebonds: {finalTelebonds}, Conversions: {finalConversions}"), 
                                args.Implanted, args.Implanted, PopupType.Medium);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles the event when a purchase is made from a store.
        /// Restores any spent Conversion currency since it's a global headrev progression score, not a spendable currency.
        /// Also updates all uplinks when a stock-limited item is purchased.
        /// </summary>
        private void OnStoreBuyFinished(ref StoreBuyFinishedEvent args)
        {
            // Get the store component
            if (!_entityManager.TryGetComponent(args.StoreUid, out StoreComponent? store))
                return;
            
            // Check if this store uses Conversion currency
            if (store.CurrencyWhitelist.Contains("Conversion"))
            {
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
                }
            }
            
            // Check if this is a stock-limited listing
            bool isStockLimited = false;
            if (args.PurchasedItem.Conditions != null)
            {
                foreach (var condition in args.PurchasedItem.Conditions)
                {
                    if (condition is Content.Shared.Store.Conditions.StockLimitedListingCondition)
                    {
                        isStockLimited = true;
                        break;
                    }
                }
            }
            
            // If this is a stock-limited listing, update all uplinks
            if (isStockLimited && store.CurrencyWhitelist.Contains("Telebond"))
            {
                // Update all uplinks with the latest stock count and last purchaser information
                UpdateAllUplinkListings();
            }
        }
        
        /// <summary>
        /// Adds Conversion currency to all head revolutionary uplinks.
        /// This is a shared counter that tracks total conversions by all head revolutionaries.
        /// </summary>
        public void AddConversionToAllHeadRevs(StoreSystem storeSystem)
    {        
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
        
        // Add Conversion to all uplinks
        foreach (var uplinkEntity in uplinkEntities)
        {
            var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } };
            var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkEntity);
        }
        
        // Show popup to all head revolutionaries (private)
        var headRevQuery = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
        foreach (var headRev in headRevQuery)
        {
            // Get the current conversion value to show in the popup
            var conversionValue = FixedPoint2.New(1); // Default to 1 if we can't find the actual value
            
            // Try to get the head revolutionary's uplink
            if (TryComp<HeadRevolutionaryImplantComponent>(headRev.Owner, out var implantComp) && 
                implantComp.ImplantUid != null && 
                EntityManager.EntityExists(implantComp.ImplantUid.Value) &&
                TryComp<StoreComponent>(implantComp.ImplantUid.Value, out var store))
            {
                conversionValue = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.New(1));
            }
            
            _popup.PopupEntity(Loc.GetString($"+1 Conversion (Total: {conversionValue})"), headRev.Owner, headRev.Owner, PopupType.Medium);
        }
        
        // Also show popup to all revolutionaries with implants
        // var revQuery = EntityManager.EntityQuery<RevolutionaryComponent, HeadRevolutionaryImplantComponent>();
        // foreach (var (_, revImplant) in revQuery)
        // {
        //     if (revImplant.ImplantUid != null && 
        //         EntityManager.EntityExists(revImplant.ImplantUid.Value) &&
        //         TryComp<StoreComponent>(revImplant.ImplantUid.Value, out var store))
        //     {
        //         var conversionValue = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.New(1));
        //         _popup.PopupEntity(Loc.GetString($"+1 Conversion (Total: {conversionValue})"), revImplant.Owner, revImplant.Owner, PopupType.Medium);
        //     }
        // }
    }
        
        /// <summary>
        /// Updates all USSP uplink listings with the current stock count and last purchaser information.
        /// </summary>
        public void UpdateAllUplinkListings()
        {            
            // Use the StoreSystem's method to update all USSP uplink UIs
            _storeSystem.UpdateAllUSSPUplinkUIs();
            
            // Also call the RevolutionaryRuleSystem to synchronize all uplinks
            var revRuleSystem = EntitySystem.Get<Content.Server.GameTicking.Rules.RevolutionaryRuleSystem>();
            
            // Get all head revolutionaries
            var headRevs = EntityManager.EntityQuery<HeadRevolutionaryComponent>();
            foreach (var headRev in headRevs)
            {
                // Synchronize all uplinks owned by this head revolutionary
                revRuleSystem.SynchronizeAllUplinksByOwner(headRev.Owner);
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
        }
    }
}
