using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class StockTradingUiState(
    List<StockCompany> entries,
    Dictionary<int, int> ownedStocks,
    float balance)
    : BoundUserInterfaceState
{
    public readonly List<StockCompany> Entries = entries;
    public readonly Dictionary<int, int> OwnedStocks = ownedStocks;
    public readonly float Balance = balance;
}

// No structure, zero fucks given
[DataDefinition, Serializable]
public partial struct StockCompany
{
    /// <summary>
    /// The displayed name of the company shown in the UI.
    /// </summary>
    [DataField(required: true)]
    public LocId? DisplayName;

    // Used for runtime-added companies that don't have a localization entry
    private string? _displayName;

    /// <summary>
    /// Gets or sets the display name, using either the localized or direct string value
    /// </summary>
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public string LocalizedDisplayName
    {
        get => _displayName ?? Loc.GetString(DisplayName ?? string.Empty);
        set => _displayName = value;
    }

    /// <summary>
    /// The current price of the company's stock
    /// </summary>
    [DataField(required: true)]
    public float CurrentPrice;

    /// <summary>
    /// The base price of the company's stock
    /// </summary>
    [DataField(required: true)]
    public float BasePrice;

    /// <summary>
    /// The price history of the company's stock
    /// </summary>
    [DataField]
    public List<float>? PriceHistory;

    public StockCompany(string displayName, float currentPrice, float basePrice, List<float>? priceHistory)
    {
        DisplayName = displayName;
        _displayName = null;
        CurrentPrice = currentPrice;
        BasePrice = basePrice;
        PriceHistory = priceHistory ?? [];
    }
}
