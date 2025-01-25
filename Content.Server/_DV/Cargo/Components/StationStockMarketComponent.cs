using System.Numerics;
using Content.Server._DV.Cargo.Systems;
using Content.Server._DV.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server._DV.Cargo.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(StockMarketSystem), typeof(StockTradingCartridgeSystem))]
public sealed partial class StationStockMarketComponent : Component
{
    /// <summary>
    /// The list of companies you can invest in
    /// </summary>
    [DataField]
    public List<StockCompany> Companies = [];

    /// <summary>
    /// The list of shares owned by the station
    /// </summary>
    [DataField]
    public Dictionary<int, int> StockOwnership = new();

    /// <summary>
    /// The interval at which the stock market updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(300); // 5 minutes

    /// <summary>
    /// The <see cref="IGameTiming.CurTime"/> timespan of next update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The sound to play after selling or buying stocks
    /// </summary>
    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The sound to play if the don't have access to buy or sell stocks
    /// </summary>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    // These work well as presets but can be changed in the yaml
    [DataField]
    public List<MarketChange> MarketChanges =
    [
        new(0.86f, new Vector2(-0.05f, 0.05f)), // Minor
        new(0.10f, new Vector2(-0.3f, 0.2f)), // Moderate
        new(0.03f, new Vector2(-0.5f, 1.5f)), // Major
        new(0.01f, new Vector2(-0.9f, 4.0f)), // Catastrophic
    ];
}

[DataRecord]
public record struct MarketChange(float Chance, Vector2 Range);
