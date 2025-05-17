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
                // Check if we have stock count in metadata
                var metadata = listingData.GetMetadata();
                if (metadata != null && metadata.TryGetValue("stock", out var stockObj) && stockObj is int stock)
                {
                    // Check if the item is out of stock
                    if (metadata.TryGetValue("outOfStock", out var outOfStockObj) && outOfStockObj is bool outOfStock && outOfStock)
                    {
                                // Replace stock count with "Out of Stock" text
                                name = Loc.GetString(listingData.Name, ("stock", Loc.GetString("store-ui-button-out-of-stock")));
                    }
                    else
                    {
                        // Use the localization with the stock count parameter
                        name = Loc.GetString(listingData.Name, ("stock", stock));
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
                                // Replace stock count with "Out of Stock" text
                                name = Loc.GetString(listingData.Name, ("stock", Loc.GetString("store-ui-button-out-of-stock")));
                            }
                            else
                            {
                                // Get the current stock count
                                var currentStock = StockLimitedListingCondition.GetCurrentStock(listingData.ID, stockCondition.StockLimit);
                                
                                // Use the localization with the stock count parameter
                                name = Loc.GetString(listingData.Name, ("stock", currentStock));
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
