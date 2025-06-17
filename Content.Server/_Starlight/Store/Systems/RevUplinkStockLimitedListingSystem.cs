using System.Linq;
using Content.Server.Store.Components;
using Content.Shared.Store.Components;
using Content.Shared.Store.Conditions;
using Content.Shared.Store.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Store.Systems;

/// <summary>
/// This system handles stock-limited listings in the store system.
/// It prevents race conditions when multiple players try to purchase the same stock-limited item.
/// This is primarily used by the revolutionary uplink system, but could be used by other systems in the future.
/// </summary>
public sealed class RevUplinkStockLimitedListingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<StorePurchaseAttemptEvent>(OnStorePurchaseAttempt);
        SubscribeLocalEvent<StorePurchaseCompletedEvent>(OnStorePurchaseCompleted);
    }
    
    /// <summary>
    /// Handles the StorePurchaseAttemptEvent for stock-limited listings.
    /// </summary>
    private void OnStorePurchaseAttempt(ref StorePurchaseAttemptEvent args)
    {
        // Get the store component
        if (!TryComp<StoreComponent>(args.StoreEntity, out var storeComp))
            return;
        
        // Find the listing
        var listingId = args.ListingId; // Store in local variable to avoid using ref parameter in lambda
        var listing = storeComp.FullListingsCatalog.FirstOrDefault(x => x.ID.Equals(listingId));
        if (listing == null)
            return;
        
        // Check if this is a stock-limited listing
        if (listing.Conditions == null)
            return;
        
        foreach (var condition in listing.Conditions)
        {
            if (condition is not StockLimitedListingCondition)
                continue;
            
            // Get the StockLimitedProcessingComponent
            if (!TryComp<StockLimitedProcessingComponent>(args.StoreEntity, out var processingComp))
            {
                processingComp = EnsureComp<StockLimitedProcessingComponent>(args.StoreEntity);
            }
            
            // Check if this listing is already being processed
            if (processingComp.ProcessingListings.TryGetValue(listing.ID, out var isProcessing) && isProcessing)
            {
                // This listing is already being processed, so cancel this purchase
                args.Cancel = true;
                return;
            }
            
            // Mark that we're processing this listing
            processingComp.ProcessingListings[listing.ID] = true;
            break;
        }
    }
    
    /// <summary>
    /// Handles the StorePurchaseCompletedEvent for stock-limited listings.
    /// </summary>
    private void OnStorePurchaseCompleted(ref StorePurchaseCompletedEvent args)
    {
        // Get the store component
        if (!TryComp<StoreComponent>(args.StoreEntity, out var storeComp))
            return;
        
        // Find the listing
        var listingId = args.ListingId; // Store in local variable to avoid using ref parameter in lambda
        var listing = storeComp.FullListingsCatalog.FirstOrDefault(x => x.ID.Equals(listingId));
        if (listing == null)
            return;
        
        // Check if this is a stock-limited listing
        if (listing.Conditions == null)
            return;
        
        foreach (var condition in listing.Conditions)
        {
            if (condition is not StockLimitedListingCondition)
                continue;
            
            // Get the StockLimitedProcessingComponent
            if (!TryComp<StockLimitedProcessingComponent>(args.StoreEntity, out var processingComp))
                return;
            
            // Mark that we're done processing this listing
            processingComp.ProcessingListings[listing.ID] = false;
            break;
        }
    }
}
