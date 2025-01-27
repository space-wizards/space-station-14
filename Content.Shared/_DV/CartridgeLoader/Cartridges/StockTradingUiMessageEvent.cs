using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class StockTradingUiMessageEvent(StockTradingUiAction action, int companyIndex, int amount)
    : CartridgeMessageEvent
{
    public readonly StockTradingUiAction Action = action;
    public readonly int CompanyIndex = companyIndex;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public enum StockTradingUiAction
{
    Buy,
    Sell,
}
