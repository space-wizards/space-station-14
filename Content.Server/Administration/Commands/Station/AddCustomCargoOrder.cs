using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Fun)]
public sealed class AddCustomCargoOrder : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "addcustomcargoorder";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 6)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var station) || !_entityManager.EntityExists(station))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        if (!_entityManager.TryGetComponent(station, out StationCargoOrderDatabaseComponent? stationCargoOrderDatabaseComponent))
        {
            shell.WriteLine(Loc.GetString("addcustomcargoorder-invalid-station"));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var orderUid) || !_entityManager.EntityExists(orderUid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        if (_entityManager.TrySystem<CargoSystem>(out var cargoSystem) && cargoSystem.AddAndApproveOrder(shell.Player?.AttachedEntity, orderUid, stationCargoOrderDatabaseComponent, 0,
                args[2], args[3], args[4], args[5]))
        {
            shell.WriteLine(Loc.GetString("addcustomcargoorder-success"));
            return;
        }

        shell.WriteLine(Loc.GetString("addcustomcargoorder-fail"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            default:
                return CompletionResult.Empty;
            case 1:
                return CompletionResult.FromHintOptions(CompletionHelper.Components<StationCargoOrderDatabaseComponent>(args[0]), Loc.GetString("shell-argument-station"));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("addcustomcargoorder-argument-order-uid"));
            case 3:
                return CompletionResult.FromHint(Loc.GetString("addcustomcargoorder-argument-order-requester"));
            case 4:
                return CompletionResult.FromHint(Loc.GetString("addcustomcargoorder-argument-order-approver"));
            case 5:
                return CompletionResult.FromHint(Loc.GetString("addcustomcargoorder-argument-order-approverJob"));
            case 6:
                return CompletionResult.FromHint(Loc.GetString("addcustomcargoorder-argument-order-reason"));
        }
    }
}
