using Content.Shared.Cargo;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Cargo.UI;

public sealed partial class CargoAppListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    private readonly GameTicker _gameTicker;
    private readonly SharedCargoSystem _cargoSystem;

    public CargoAppListing()
    {
        IoCManager.InjectDependencies(this);
        _gameTicker = _entitySystem.GetEntitySystem<GameTicker>();
        _cargoSystem = _entitySystem.GetEntitySystem<SharedCargoSystem>();
    }

    public void AddCargoAppEntries(CargoOrderData[] orders)
    {
        foreach (var order in orders)
        {
            var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
            AddChild(new CargoAppEntry
            {
                Separator = { MinWidth = 0 },
                Time = { Text = stationTime.ToString("hh\\:mm\\:ss") },
                Requester = { Text = order.Requester },
                PointCost = { Text = Loc.GetString("cargo-console-menu-points-amount", ("amount", _cargoSystem.GetBasketTotalCost(order.Basket).ToString())) },
            });
        }
    }
}
