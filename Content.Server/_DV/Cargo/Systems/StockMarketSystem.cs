using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server._DV.Cargo.Components;
using Content.Server._DV.CartridgeLoader.Cartridges;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._DV.Cargo.Systems;

/// <summary>
/// This handles the stock market updates
/// </summary>
public sealed class StockMarketSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;
    private const float MaxPrice = 262144; // 1/64 of max safe integer

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _log.GetSawmill("admin.stock_market");

        SubscribeLocalEvent<StockTradingCartridgeComponent, CartridgeMessageEvent>(OnStockTradingMessage);
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<StationStockMarketComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (curTime < component.NextUpdate)
                continue;

            component.NextUpdate = curTime + component.UpdateInterval;
            UpdateStockPrices(uid, component);
        }
    }

    private void OnStockTradingMessage(Entity<StockTradingCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not StockTradingUiMessageEvent message)
            return;

        var user = args.Actor;
        var companyIndex = message.CompanyIndex;
        var amount = message.Amount;
        var loader = GetEntity(args.LoaderUid);

        // Ensure station and stock market components are valid
        if (ent.Comp.Station is not {} station || !TryComp<StationStockMarketComponent>(station, out var stockMarket))
            return;

        // Validate company index
        if (companyIndex < 0 || companyIndex >= stockMarket.Companies.Count)
            return;

        if (!TryComp<AccessReaderComponent>(ent, out var access))
            return;

        // Attempt to retrieve ID card from loader,
        // play deny sound and exit if access is not allowed
        if (!_idCard.TryGetIdCard(loader, out var idCard) || !_access.IsAllowed(idCard, ent.Owner, access))
        {
            _audio.PlayEntity(stockMarket.DenySound, user, loader);
            _popup.PopupEntity(Loc.GetString("stock-trading-access-denied"), user, user);
            return;
        }

        try
        {
            var company = stockMarket.Companies[companyIndex];

            // Attempt to buy or sell stocks based on the action
            bool success;
            switch (message.Action)
            {
                case StockTradingUiAction.Buy:
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):user} attempting to buy {amount} stocks of {company.LocalizedDisplayName}");
                    success = TryChangeStocks(station, stockMarket, companyIndex, amount, user);
                    break;

                case StockTradingUiAction.Sell:
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):user} attempting to sell {amount} stocks of {company.LocalizedDisplayName}");
                    success = TryChangeStocks(station, stockMarket, companyIndex, -amount, user);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Play confirmation sound if the transaction was successful
            _audio.PlayEntity(success ? stockMarket.ConfirmSound : stockMarket.DenySound, user, loader);
            if (!success)
            {
                _popup.PopupEntity(Loc.GetString("stock-trading-transaction-failed"), user, user);
            }
        }
        finally
        {
            // Raise the event to update the UI regardless of outcome
            UpdateStockMarket(station);
        }
    }

    private void UpdateStockMarket(EntityUid station)
    {
        var ev = new StockMarketUpdatedEvent(station);
        RaiseLocalEvent(ref ev);
    }

    private bool TryChangeStocks(
        EntityUid station,
        StationStockMarketComponent stockMarket,
        int companyIndex,
        int amount,
        EntityUid user)
    {
        if (amount == 0 || companyIndex < 0 || companyIndex >= stockMarket.Companies.Count)
            return false;

        // Check if the station has a bank account
        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return false;

        var company = stockMarket.Companies[companyIndex];
        var totalValue = (int)Math.Round(company.CurrentPrice * amount);

        if (!stockMarket.StockOwnership.TryGetValue(companyIndex, out var currentOwned))
            currentOwned = 0;

        if (amount > 0)
        {
            // Buying: see if we can afford it
            if (bank.Balance < totalValue)
                return false;
        }
        else
        {
            // Selling: see if we have enough stocks to sell
            var selling = -amount;
            if (currentOwned < selling)
                return false;
        }

        var newAmount = currentOwned + amount;
        if (newAmount > 0)
            stockMarket.StockOwnership[companyIndex] = newAmount;
        else
            stockMarket.StockOwnership.Remove(companyIndex);

        // Update the bank account (take away for buying and give for selling)
        _cargo.UpdateBankAccount(station, -totalValue);

        // Log the transaction
        var verb = amount > 0 ? "bought" : "sold";
        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"[StockMarket] {ToPrettyString(user):user} {verb} {Math.Abs(amount)} stocks of {company.LocalizedDisplayName} at {company.CurrentPrice:F2} credits each (Total: {totalValue})");

        return true;
    }

    private void UpdateStockPrices(EntityUid station, StationStockMarketComponent stockMarket)
    {
        for (var i = 0; i < stockMarket.Companies.Count; i++)
        {
            var company = stockMarket.Companies[i];
            var changeType = DetermineMarketChange(stockMarket.MarketChanges);
            var multiplier = CalculatePriceMultiplier(changeType);

            UpdatePriceHistory(ref company);

            // Update price with multiplier
            var oldPrice = company.CurrentPrice;
            company.CurrentPrice *= (1 + multiplier);

            // Ensure price doesn't go below minimum threshold
            company.CurrentPrice = MathF.Max(company.CurrentPrice, company.BasePrice * 0.1f);

            // Ensure price doesn't go above maximum threshold
            company.CurrentPrice = MathF.Min(company.CurrentPrice, MaxPrice);

            stockMarket.Companies[i] = company;

            // Calculate the percentage change
            var percentChange = (company.CurrentPrice - oldPrice) / oldPrice * 100;

            // Raise the event
            UpdateStockMarket(station);

            // Log it
            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"[StockMarket] Company '{company.LocalizedDisplayName}' price updated by {percentChange:+0.00;-0.00}% from {oldPrice:0.00} to {company.CurrentPrice:0.00}");
        }
    }

    /// <summary>
    /// Attempts to change the price for a specific company
    /// </summary>
    /// <returns>True if the operation was successful, false otherwise</returns>
    public bool TryChangeStocksPrice(EntityUid station,
        StationStockMarketComponent stockMarket,
        float newPrice,
        int companyIndex)
    {
        // Check if it exceeds the max price
        if (newPrice > MaxPrice)
        {
            _sawmill.Error($"New price cannot be greater than {MaxPrice}.");
            return false;
        }

        if (companyIndex < 0 || companyIndex >= stockMarket.Companies.Count)
            return false;

        var company = stockMarket.Companies[companyIndex];
        UpdatePriceHistory(ref company);

        company.CurrentPrice = MathF.Max(newPrice, company.BasePrice * 0.1f);
        stockMarket.Companies[companyIndex] = company;

        UpdateStockMarket(station);
        return true;
    }

    /// <summary>
    /// Attempts to add a new company to the station
    /// </summary>
    /// <returns>False if the company already exists, true otherwise</returns>
    public bool TryAddCompany(EntityUid station,
        StationStockMarketComponent stockMarket,
        float basePrice,
        string displayName)
    {
        // Create a new company struct with the specified parameters
        var company = new StockCompany
        {
            LocalizedDisplayName = displayName, // Assume there's no Loc for it
            BasePrice = basePrice,
            CurrentPrice = basePrice,
            PriceHistory = [],
        };

        UpdatePriceHistory(ref company);
        stockMarket.Companies.Add(company);

        UpdateStockMarket(station);

        return true;
    }

    /// <summary>
    /// Attempts to add a new company to the station using the StockCompany
    /// </summary>
    /// <returns>False if the company already exists, true otherwise</returns>
    public bool TryAddCompany(Entity<StationStockMarketComponent> station,
        StockCompany company)
    {
        // Make sure it has a price history
        UpdatePriceHistory(ref company);

        // Add the new company to the dictionary
        station.Comp.Companies.Add(company);

        UpdateStockMarket(station);

        return true;
    }

    private static void UpdatePriceHistory(ref StockCompany company)
    {
        // Create if null
        company.PriceHistory ??= [];

        // Make sure it has at least 5 entries
        while (company.PriceHistory.Count < 5)
        {
            company.PriceHistory.Add(company.BasePrice);
        }

        // Store previous price in history
        company.PriceHistory.Add(company.CurrentPrice);

        if (company.PriceHistory.Count > 5) // Keep last 5 prices
            company.PriceHistory.RemoveAt(1); // Always keep the base price
    }

    private MarketChange DetermineMarketChange(List<MarketChange> marketChanges)
    {
        var roll = _random.NextFloat();
        var cumulative = 0f;

        foreach (var change in marketChanges)
        {
            cumulative += change.Chance;
            if (roll <= cumulative)
                return change;
        }

        return marketChanges[0]; // Default to first (usually minor) change if we somehow exceed 100%
    }

    private float CalculatePriceMultiplier(MarketChange change)
    {
        // Using Box-Muller transform for normal distribution
        var u1 = _random.NextFloat();
        var u2 = _random.NextFloat();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

        // Scale and shift the result to our desired range
        var range = change.Range.Y - change.Range.X;
        var mean = (change.Range.Y + change.Range.X) / 2;
        var stdDev = range / 6.0f; // 99.7% of values within range

        var result = (float)(mean + (stdDev * randStdNormal));
        return Math.Clamp(result, change.Range.X, change.Range.Y);
    }
}

/// <summary>
/// Broadcast whenever a stock market is updated.
/// </summary>
[ByRefEvent]
public record struct StockMarketUpdatedEvent(EntityUid Station);
