namespace Content.Server._DV.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(StockTradingCartridgeSystem))]
public sealed partial class StockTradingCartridgeComponent : Component
{
    /// <summary>
    /// Station entity to keep track of
    /// </summary>
    [DataField]
    public EntityUid? Station;
}
