// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Shared.Backmen.Economy;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Backmen.Economy.Wage;

public sealed class WagePaydayEvent : EntityEventArgs
{
    public FixedPoint2 Mod { get; set; } = 1;
    public FixedPoint2? Value { get; set; } = null;
    public readonly HashSet<Entity<BankAccountComponent>> WhiteListTo = new();
}

public sealed record WagePaydayPayout(
    uint Id,
    Entity<BankAccountComponent> FromAccountNumber,
    Entity<BankAccountComponent> ToAccountNumber)
{
    public FixedPoint2 PayoutAmount { get; set; }
}

public sealed class WageManagerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly BankManagerSystem _bankManagerSystem = default!;

    private uint _nextId = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    public readonly HashSet<WagePaydayPayout> PayoutsList = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public bool WagesEnabled { get; private set; }
    //private void SetEnabled(bool value) => WagesEnabled = value;
    private void SetEnabled(bool value)
    {
        WagesEnabled = value;
    }
    public override void Initialize()
    {
        base.Initialize();
        _configurationManager.OnValueChanged(Shared.Backmen.CCVar.CCVars.EconomyWagesEnabled, SetEnabled, true);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<WagePaydayEvent>(OnPayday);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        PayoutsList.Clear();
        _nextId = 1;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configurationManager.UnsubValueChanged(Shared.Backmen.CCVar.CCVars.EconomyWagesEnabled, SetEnabled);
    }

    public void OnPayday(WagePaydayEvent ev)
    {
        foreach (var payout in PayoutsList.ToArray())
        {
            // бонусная зп на отдел?
            if (ev.WhiteListTo.Count > 0 && !ev.WhiteListTo.Contains(payout.ToAccountNumber))
            {
                continue;
            }
            var val = ev.Value ?? payout.PayoutAmount;

            if (TerminatingOrDeleted(payout.ToAccountNumber) || TerminatingOrDeleted(payout.FromAccountNumber))
            {
                PayoutsList.Remove(payout);
                continue;
            }

            _bankManagerSystem.TryTransferFromToBankAccount(
                payout.FromAccountNumber,
                payout.ToAccountNumber,
                val * ev.Mod);
        }
    }

    public bool TryAddAccountToWagePayoutList(Entity<BankAccountComponent> bankAccount, JobPrototype jobPrototype)
    {
        if (jobPrototype.WageDepartment == null ||
            !_prototypeManager.TryIndex(jobPrototype.WageDepartment, out DepartmentPrototype? department))
            return false;

        if (!_bankManagerSystem.TryGetBankAccount(department.AccountNumber, out var departmentBankAccount))
            return false;

        var newPayout = new WagePaydayPayout(_nextId++, departmentBankAccount.Value, bankAccount)
        {
            PayoutAmount = jobPrototype.Wage
        };

    PayoutsList.Add(newPayout);
        return true;
    }
}
