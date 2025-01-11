using Content.Server.Administration;
using Content.Server._DV.Cargo.Components;
using Content.Server._DV.Cargo.Systems;
using Content.Shared.Administration;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Console;

namespace Content.Server._DV.Cargo;

[AdminCommand(AdminFlags.Fun)]
public sealed class ChangeStocksPriceCommand : IConsoleCommand
{
    public string Command => "changestocksprice";
    public string Description => Loc.GetString("cmd-changestocksprice-desc");
    public string Help => Loc.GetString("cmd-changestocksprice-help", ("command", Command));

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var companyIndex))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        if (!float.TryParse(args[1], out var newPrice))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        EntityUid? targetStation = null;
        if (args.Length > 2)
        {
            if (!EntityUid.TryParse(args[2], out var station))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }
            targetStation = station;
        }

        var stockMarket = _entitySystemManager.GetEntitySystem<StockMarketSystem>();
        var query = _entityManager.EntityQueryEnumerator<StationStockMarketComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // Skip if we're looking for a specific station and this isn't it
            if (targetStation != null && uid != targetStation)
                continue;

            if (stockMarket.TryChangeStocksPrice(uid, comp, newPrice, companyIndex))
            {
                shell.WriteLine(Loc.GetString("shell-command-success"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-changestocksprice-invalid-company"));
            return;
        }

        shell.WriteLine(targetStation != null
            ? Loc.GetString("cmd-changestocksprice-invalid-station")
            : Loc.GetString("cmd-changestocksprice-no-stations"));
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddStocksCompanyCommand : IConsoleCommand
{
    public string Command => "addstockscompany";
    public string Description => Loc.GetString("cmd-addstockscompany-desc");
    public string Help => Loc.GetString("cmd-addstockscompany-help", ("command", Command));

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!float.TryParse(args[1], out var basePrice))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        EntityUid? targetStation = null;
        if (args.Length > 2)
        {
            if (!EntityUid.TryParse(args[2], out var station))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }
            targetStation = station;
        }

        var displayName = args[0];
        var stockMarket = _entitySystemManager.GetEntitySystem<StockMarketSystem>();
        var query = _entityManager.EntityQueryEnumerator<StationStockMarketComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // Skip if we're looking for a specific station and this isn't it
            if (targetStation != null && uid != targetStation)
                continue;

            if (stockMarket.TryAddCompany(uid, comp, basePrice, displayName))
            {
                shell.WriteLine(Loc.GetString("shell-command-success"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-addstockscompany-failure"));
            return;
        }

        shell.WriteLine(targetStation != null
            ? Loc.GetString("cmd-addstockscompany-invalid-station")
            : Loc.GetString("cmd-addstockscompany-no-stations"));
    }
}
