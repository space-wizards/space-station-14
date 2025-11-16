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

        foreach (var (account, _) in _cargo.GetAccounts(station))
        {
            yield return new BankAccount(account.Id, station, _cargo, EntityManager);
        }
    }

    [CommandImplementation("accounts")]
    public IEnumerable<BankAccount> Accounts([PipedArgument] IEnumerable<EntityUid> stations)
        => stations.SelectMany(Accounts);

    [CommandImplementation("account")]
    public BankAccount Account([PipedArgument] EntityUid station, ProtoId<CargoAccountPrototype> account)
    {
        _cargo ??= GetSys<CargoSystem>();

        return new BankAccount(account.Id, station, _cargo, EntityManager);
    }

    [CommandImplementation("account")]
    public IEnumerable<BankAccount> Account([PipedArgument] IEnumerable<EntityUid> stations, ProtoId<CargoAccountPrototype> account)
        => stations.Select(x => Account(x, account));

    [CommandImplementation("adjust")]
    public IEnumerable<BankAccount> Adjust([PipedArgument] IEnumerable<BankAccount> @ref, int by)
    {
        _cargo ??= GetSys<CargoSystem>();
        var bankAccounts = @ref.ToList();
        foreach (var bankAccount in bankAccounts.ToList())
        {
            _cargo.TryAdjustBankAccount(bankAccount.Station, bankAccount.Account, by, true);
        }
        return bankAccounts;
    }

    [CommandImplementation("set")]
    public IEnumerable<BankAccount> Set([PipedArgument] IEnumerable<BankAccount> @ref, int by)
    {
        _cargo ??= GetSys<CargoSystem>();
        var bankAccounts = @ref.ToList();
        foreach (var bankAccount in bankAccounts.ToList())
        {
            _cargo.TrySetBankAccount(bankAccount.Station, bankAccount.Account, by, true);
        }
        return bankAccounts;
    }

    [CommandImplementation("amount")]
    public IEnumerable<int> Amount([PipedArgument] IEnumerable<BankAccount> @ref)
    {
        _cargo ??= GetSys<CargoSystem>();
        return @ref.Select(bankAccount => (success: _cargo.TryGetAccount(bankAccount.Station, bankAccount.Account, out var money), money))
        .Where(result => result.success)
        .Select(result => result.money);
    }
}

public readonly record struct BankAccount(
    string Account,
    Entity<StationBankAccountComponent?> Station,
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
