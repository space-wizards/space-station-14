using System.Linq;
using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Shared.Administration;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Station.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class BankCommand : ToolshedCommand
{
    private CargoSystem? _cargo;

    [CommandImplementation("accounts")]
    public IEnumerable<BankAccount> Accounts([PipedArgument] EntityUid station)
    {
        _cargo ??= GetSys<CargoSystem>();

        if (!TryComp<StationBankAccountComponent>(station, out var bankAccount))
            yield break;

        var stationEnt = (Entity<StationBankAccountComponent>)(station, bankAccount);

        foreach (var (account, _) in _cargo.GetAccounts(stationEnt))
        {
            yield return new BankAccount(account.Id, stationEnt, _cargo, EntityManager);
        }
    }

    [CommandImplementation("accounts")]
    public IEnumerable<BankAccount> Accounts([PipedArgument] IEnumerable<EntityUid> stations)
        => stations.SelectMany(Accounts);

    [CommandImplementation("account")]
    public BankAccount? Account([PipedArgument] EntityUid station, ProtoId<CargoAccountPrototype> account)
    {
        _cargo ??= GetSys<CargoSystem>();

        if (!TryComp<StationBankAccountComponent>(station, out var bankAccount))
            return null;

        var stationEnt = (Entity<StationBankAccountComponent>)(station, bankAccount);

        return new BankAccount(account.Id, stationEnt, _cargo, EntityManager);
    }

    [CommandImplementation("account")]
    public IEnumerable<BankAccount> Account([PipedArgument] IEnumerable<EntityUid> stations, ProtoId<CargoAccountPrototype> account)
        => stations.Select(x => Account(x, account).GetValueOrDefault());

    [CommandImplementation("adjust")]
    public BankAccount Adjust([PipedArgument] BankAccount @ref, int by)
    {
        _cargo ??= GetSys<CargoSystem>();
        _cargo.TryAdjustBankAccount(@ref.Station, @ref.Account, by, true);
        return @ref;
    }

    [CommandImplementation("adjust")]
    public IEnumerable<BankAccount> Adjust([PipedArgument] IEnumerable<BankAccount> @ref, int by)
        => @ref.Select(x => Adjust(x, by));

    [CommandImplementation("set")]
    public BankAccount Set([PipedArgument] BankAccount @ref, int by)
    {
        _cargo ??= GetSys<CargoSystem>();
        _cargo.TrySetBankAccount(@ref.Station, @ref.Account, by, true);
        return @ref;
    }

    [CommandImplementation("set")]
    public IEnumerable<BankAccount> Set([PipedArgument] IEnumerable<BankAccount> @ref, int by)
        => @ref.Select(x => Set(x, by));

    [CommandImplementation("amount")]
    public int Ammount([PipedArgument] BankAccount @ref)
    {
        _cargo ??= GetSys<CargoSystem>();
        _cargo.TryGetAccount(@ref.Station, @ref.Account, out var money);
        return money;
    }

    [CommandImplementation("amount")]
    public IEnumerable<int> Ammount([PipedArgument] IEnumerable<BankAccount> @ref)
        => @ref.Select(Ammount);
}

public readonly record struct BankAccount(
    string Account,
    Entity<StationBankAccountComponent> Station,
    CargoSystem Cargo,
    IEntityManager EntityManager)
{
    public override string ToString()
    {
        if (!Cargo.TryGetAccount(Station, Account, out var money))
        {
            return $"{EntityManager.ToPrettyString(Station)} Account {Account} : (not a account)";
        }

        return $"{EntityManager.ToPrettyString(Station)} Account {Account} : {money}";
    }
}
