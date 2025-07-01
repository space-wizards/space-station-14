using System.Collections.Generic;

namespace Content.Shared.Store;

/// <summary>
/// Extensions for the ListingData class to add metadata support.
/// </summary>
public static class ListingDataExtensions
{
    private static readonly Dictionary<string, Dictionary<string, object>> _metadata = new();

    /// <summary>
    /// Gets the metadata for a listing.
    /// </summary>
    public static Dictionary<string, object>? GetMetadata(this ListingData listing)
    {
        if (_metadata.TryGetValue(listing.ID, out var metadata))
            return metadata;
        
        return null;
    }

    /// <summary>
    /// Sets the metadata for a listing.
    /// </summary>
    public static void SetMetadata(this ListingData listing, Dictionary<string, object> metadata)
    {
        _metadata[listing.ID] = metadata;
    }

    /// <summary>
    /// Gets or creates the metadata for a listing.
    /// </summary>
    public static Dictionary<string, object> GetOrCreateMetadata(this ListingData listing)
    {
        if (!_metadata.TryGetValue(listing.ID, out var metadata))
        {
            metadata = new Dictionary<string, object>();
            _metadata[listing.ID] = metadata;
        }
        
        return metadata;
    }
}

/// <summary>
/// Extension methods for the ListingData class to add a Metadata property.
/// </summary>
public static class ListingDataMetadataExtensions
{
    /// <summary>
    /// Gets or sets the metadata for a listing.
    /// </summary>
    public static Dictionary<string, object>? GetOrSetMetadata(this ListingData listing, Dictionary<string, object>? value = null)
    {
        if (value == null)
            return ListingDataExtensions.GetMetadata(listing);
        
        ListingDataExtensions.SetMetadata(listing, value);
        return value;
    }
}
