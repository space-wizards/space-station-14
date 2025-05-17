using Content.Shared.Store.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public static class ListingLocalisationHelpers
{
    /// <summary>
    /// ListingData's Name field can be either a localisation string or the actual entity's name.
    /// This function gets a localised name from the localisation string if it exists, and if not, it gets the entity's name.
    /// If neither a localised string exists, or an associated entity name, it will return the value of the "Name" field.
    /// </summary>
    public static string GetLocalisedNameOrEntityName(ListingData listingData, IPrototypeManager prototypeManager)
    {
        var name = string.Empty;

        if (listingData.Name != null)
        {
            // Check if this is a localized name that needs stock count
            if (listingData.Name.Contains("-name"))
            {
                // If the name has already been modified by StockLimitedListingCondition, just return it
                if (listingData.Name.Contains("(") && listingData.Name.Contains("/") && listingData.Name.Contains(")"))
                {
                    return listingData.Name;
                }
                
                // Check if we have stock count in metadata
                var metadata = listingData.GetMetadata();
                if (metadata != null && metadata.TryGetValue("stock", out var stockObj) && stockObj is int stock)
                {
                    // Get the stock limit if available
                    int stockLimit = 4; // Default to 4
                    if (metadata.TryGetValue("stockLimit", out var stockLimitObj) && stockLimitObj is int limit)
                    {
                        stockLimit = limit;
                    }
                    
                    // Check if the item is out of stock
                    if (metadata.TryGetValue("outOfStock", out var outOfStockObj) && outOfStockObj is bool outOfStock && outOfStock)
                    {
                        // Format with X/Y format
                        var baseName = Loc.GetString(listingData.Name);
                        name = $"{baseName} (0/{stockLimit})";
                    }
                    else
                    {
                        // Format with X/Y format
                        var baseName = Loc.GetString(listingData.Name);
                        name = $"{baseName} ({stock}/{stockLimit})";
                    }
                    
                    return name;
                }
                // Fallback to checking conditions
                else if (listingData.Conditions != null)
                {
                    foreach (var condition in listingData.Conditions)
                    {
                        if (condition is StockLimitedListingCondition stockCondition)
                        {
                            // Check if the item is out of stock
                            if (StockLimitedListingCondition.IsOutOfStock(listingData.ID))
                            {
                                // Format with X/Y format
                                var baseName = Loc.GetString(listingData.Name);
                                name = $"{baseName} (0/{stockCondition.StockLimit})";
                            }
                            else
                            {
                                // Get the current stock count
                                var currentStock = StockLimitedListingCondition.GetCurrentStock(listingData.ID, stockCondition.StockLimit);
                                
                                // Format with X/Y format
                                var baseName = Loc.GetString(listingData.Name);
                                name = $"{baseName} ({currentStock}/{stockCondition.StockLimit})";
                            }
                            
                            return name;
                        }
                    }
                }
            }
            
            name = Loc.GetString(listingData.Name);
        }
        else if (listingData.ProductEntity != null)
            name = prototypeManager.Index(listingData.ProductEntity.Value).Name;

        return name;
    }

    /// <summary>
    /// ListingData's Description field can be either a localisation string or the actual entity's description.
    /// This function gets a localised description from the localisation string if it exists, and if not, it gets the entity's description.
    /// If neither a localised string exists, or an associated entity description, it will return the value of the "Description" field.
    /// </summary>
    public static string GetLocalisedDescriptionOrEntityDescription(ListingData listingData, IPrototypeManager prototypeManager)
    {
        var desc = string.Empty;

        if (listingData.Description != null)
        {
            // Check if this is a localized description that needs last purchaser info
            if (listingData.Description.Contains("-desc"))
            {
                // Check if we have stock count and last purchaser in metadata
                var metadata = listingData.GetMetadata();
                if (metadata != null && 
                    metadata.TryGetValue("stock", out var stockObj) && stockObj is int stock &&
                    metadata.TryGetValue("lastPurchaser", out var lastPurchaserObj) && lastPurchaserObj is string purchaserText)
                {
                    // Check if the item is out of stock
                    if (metadata.TryGetValue("outOfStock", out var outOfStockObj) && outOfStockObj is bool outOfStock && outOfStock)
                    {
                        // Use the localization with "Out of Stock" and last purchaser parameters
                        desc = Loc.GetString(listingData.Description, 
                            ("stock", Loc.GetString("store-ui-button-out-of-stock")),
                            ("lastPurchaser", purchaserText)
                        );
                    }
                    else
                    {
                        // Use the localization with the stock count and last purchaser parameters
                        desc = Loc.GetString(listingData.Description, 
                            ("stock", stock),
                            ("lastPurchaser", purchaserText)
                        );
                    }
                    
                    return desc;
                }
                // Fallback to checking conditions
                else if (listingData.Conditions != null)
                {
                    foreach (var condition in listingData.Conditions)
                    {
                        if (condition is StockLimitedListingCondition stockCondition)
                        {
                            // Get the last purchaser
                            var lastPurchaser = StockLimitedListingCondition.GetLastPurchaser(listingData.ID);
                            
                            // Format the last purchaser string
                            var lastPurchaserText = string.IsNullOrEmpty(lastPurchaser) 
                                ? "" 
                                : Loc.GetString(" Last purchased by: {0}", ("name", lastPurchaser));
                            
                            // Check if the item is out of stock
                            if (StockLimitedListingCondition.IsOutOfStock(listingData.ID))
                            {
                                // Use the localization with "Out of Stock" and last purchaser parameters
                                desc = Loc.GetString(listingData.Description, 
                                    ("stock", Loc.GetString("store-ui-button-out-of-stock")),
                                    ("lastPurchaser", lastPurchaserText)
                                );
                            }
                            else
                            {
                                // Get the current stock count
                                var currentStock = StockLimitedListingCondition.GetCurrentStock(listingData.ID, stockCondition.StockLimit);
                                
                                // Use the localization with the stock count and last purchaser parameters
                                desc = Loc.GetString(listingData.Description, 
                                    ("stock", currentStock),
                                    ("lastPurchaser", lastPurchaserText)
                                );
                            }
                            
                            return desc;
                        }
                    }
                }
            }
            
            desc = Loc.GetString(listingData.Description);
        }
        else if (listingData.ProductEntity != null)
            desc = prototypeManager.Index(listingData.ProductEntity.Value).Description;

        return desc;
    }
}
