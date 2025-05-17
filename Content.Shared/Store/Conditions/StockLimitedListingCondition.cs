using System.Collections.Generic;
using Content.Shared.IdentityManagement;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// A condition that limits the number of times an item can be purchased globally.
/// Also tracks the last purchaser of the item.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class StockLimitedListingCondition : ListingCondition
{
    /// <summary>
    /// The maximum number of times this item can be purchased globally.
    /// </summary>
    [DataField("stockLimit")]
    public int StockLimit = 1;

    /// <summary>
    /// The current number of times this item has been purchased.
    /// </summary>
    [DataField("currentStock")]
    public int CurrentStock = 0;

    /// <summary>
    /// The name of the last person to purchase this item.
    /// </summary>
    [DataField("lastPurchaser")]
    public string? LastPurchaser = null;

    /// <summary>
    /// Dictionary to track which listings have been modified by this condition.
    /// Key is the listing ID, value is whether the listing has been modified.
    /// </summary>
    private static readonly Dictionary<string, bool> _modifiedListings = new();

    /// <summary>
    /// Dictionary to track the current stock of each listing.
    /// Key is the listing ID, value is the current stock.
    /// </summary>
    private static readonly Dictionary<string, int> _stockCounts = new();

    /// <summary>
    /// Dictionary to track the stock limit of each listing.
    /// Key is the listing ID, value is the maximum stock.
    /// </summary>
    private static readonly Dictionary<string, int> _stockLimits = new();

    /// <summary>
    /// Dictionary to track the last purchaser of each listing.
    /// Key is the listing ID, value is the name of the last purchaser.
    /// </summary>
    private static readonly Dictionary<string, string> _lastPurchasers = new();
    
    /// <summary>
    /// Dictionary to track whether a listing is out of stock.
    /// Key is the listing ID, value is whether the listing is out of stock.
    /// </summary>
    private static readonly Dictionary<string, bool> _outOfStock = new();

    public override bool Condition(ListingConditionArgs args)
    {
        var listingId = args.Listing.ID;

        // Store the stock limit for this listing
        _stockLimits[listingId] = StockLimit;

        // Initialize stock count for this listing if it doesn't exist
        if (!_stockCounts.ContainsKey(listingId))
        {
            // Use the StockLimit from the catalog
            _stockCounts[listingId] = StockLimit;
            CurrentStock = StockLimit; // Initialize CurrentStock
            
            // Log the initialization
            Logger.InfoS("stock-limited", $"Initialized stock count for {listingId} to {StockLimit} from catalog");
        }
        else
        {
            // Update CurrentStock from the static dictionary
            CurrentStock = _stockCounts[listingId];
        }

        // Update the listing name and description with stock count and last purchaser
        UpdateListingInfo(args.Listing, listingId);
        _modifiedListings[listingId] = true;

        // Check if we've reached the stock limit
        var hasStock = _stockCounts[listingId] > 0;
        
        // Update the out of stock status
        _outOfStock[listingId] = !hasStock;
        
        // If out of stock, mark the listing as unavailable but still return true
        // so it shows up in the listing but is greyed out
        if (!hasStock)
        {
            args.Listing.Unavailable = true;
            return false;
        }
        
        // Always return true if we have stock
        return true;
    }

    /// <summary>
    /// Updates the listing name and description with the current stock count and last purchaser.
    /// </summary>
    private void UpdateListingInfo(ListingData listing, string listingId)
    {
        // Get the current stock count
        var currentStock = _stockCounts.ContainsKey(listingId) ? _stockCounts[listingId] : StockLimit;
        
        // Get the stock limit
        var stockLimit = _stockLimits.ContainsKey(listingId) ? _stockLimits[listingId] : StockLimit;

        // Get the last purchaser
        var lastPurchaser = _lastPurchasers.ContainsKey(listingId) ? _lastPurchasers[listingId] : null;

        // Store the stock count in the listing's metadata
        var metadata = listing.GetOrCreateMetadata();
        
        metadata["stock"] = currentStock;
        metadata["stockLimit"] = stockLimit;
        
        // Store the out of stock status in the metadata
        var outOfStock = currentStock <= 0;
        metadata["outOfStock"] = outOfStock;
        
        // Update the CurrentStock property
        CurrentStock = currentStock;
        
        // Update the LastPurchaser property
        LastPurchaser = lastPurchaser;
        
        // Directly update the name with the stock count
        if (listing.Name != null && listing.Name.Contains("-name"))
        {
            // Get the base name without the stock count
            var baseName = Loc.GetString(listing.Name);
            
            // Format the name with the stock count
            if (outOfStock)
            {
                listing.Name = $"{baseName} (0/{stockLimit})";
            }
            else
            {
                listing.Name = $"{baseName} ({currentStock}/{stockLimit})";
            }
        }
        
        // Directly update the description with the last purchaser
        if (listing.Description != null && listing.Description.Contains("-desc"))
        {
            // Get the base description without the last purchaser
            var baseDesc = Loc.GetString(listing.Description);
            
            // Format the description with the last purchaser
            if (!string.IsNullOrEmpty(lastPurchaser))
            {
                listing.Description = $"{baseDesc}\nLast purchased by: {lastPurchaser}";
            }
        }
    }

    /// <summary>
    /// Called when an item is purchased to update the stock count and last purchaser.
    /// </summary>
    public static void OnItemPurchased(string listingId, string purchaserName)
    {
        // Get the current stock count and stock limit
        var currentStock = _stockCounts.ContainsKey(listingId) ? _stockCounts[listingId] : 0;
        
        // Get the stock limit from the _stockLimits dictionary
        var stockLimit = _stockLimits.ContainsKey(listingId) ? _stockLimits[listingId] : 4;
        
        // Decrement the stock count
        _stockCounts[listingId] = Math.Max(0, currentStock - 1);

        // Update the last purchaser
        _lastPurchasers[listingId] = purchaserName;
        
        // Update the out of stock status
        _outOfStock[listingId] = _stockCounts[listingId] <= 0;
        
        // Log the purchase for debugging
        Logger.InfoS("stock-limited", $"Item {listingId} purchased by {purchaserName}. New stock: {_stockCounts[listingId]}/{stockLimit}");
    }
    
    /// <summary>
    /// Initializes the stock count for a listing if it doesn't exist.
    /// </summary>
    public static void InitializeStockCount(string listingId, int stockLimit)
    {
        if (!_stockCounts.ContainsKey(listingId))
        {
            _stockCounts[listingId] = stockLimit;
            _stockLimits[listingId] = stockLimit;
            Logger.InfoS("stock-limited", $"Initialized stock count for {listingId} to {stockLimit}");
        }
    }

    /// <summary>
    /// Gets the current stock count for a listing.
    /// </summary>
    public static int GetCurrentStock(string listingId, int defaultStockLimit)
    {
        return _stockCounts.ContainsKey(listingId) ? _stockCounts[listingId] : defaultStockLimit;
    }

    /// <summary>
    /// Gets the last purchaser for a listing.
    /// </summary>
    public static string? GetLastPurchaser(string listingId)
    {
        return _lastPurchasers.ContainsKey(listingId) ? _lastPurchasers[listingId] : null;
    }
    
    /// <summary>
    /// Checks if a listing is out of stock.
    /// </summary>
    public static bool IsOutOfStock(string listingId)
    {
        return _outOfStock.ContainsKey(listingId) && _outOfStock[listingId];
    }
}
