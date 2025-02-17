// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Backmen.CartridgeLoader.Cartridges;
using Content.Server.Backmen.Economy.ATM;
using Content.Shared.Backmen.Economy;
using Content.Shared.Backmen.Economy.ATM;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Backmen.Economy;

public sealed class BankManagerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ATMSystem _atmSystem = default!;

    [ViewVariables] public readonly Dictionary<string, Entity<BankAccountComponent>> ActiveBankAccounts = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<BankAccountComponent, BankChangeBalanceEvent>(OnBalanceChange);
        SubscribeLocalEvent<BankAccountComponent, ComponentGetStateAttemptEvent>(IsCardInHand);
    }

    private void IsCardInHand(Entity<BankAccountComponent> component, ref ComponentGetStateAttemptEvent args)
    {

    }

    private void OnBalanceChange(Entity<BankAccountComponent> component, ref BankChangeBalanceEvent args)
    {
        if (args.Handled)
        {
            return;
        }
        args.Handled = true;

        if (component.Comp.IsInfinite)
        {
            return;
        }

        component.Comp.SetBalance(args.Balance);
        Dirty(component);

        var ev = new ChangeBankAccountBalanceEvent(args.Balance - args.OldBalance, args.Balance);
        RaiseLocalEvent(component, ev);

        var parent = Transform(component).ParentUid;
        if (parent.IsValid() && HasComp<AtmComponent>(parent))
        {
            _atmSystem.UpdateUi(parent, component);
        }
    }

    public bool TryChangeBalanceBy(Entity<BankAccountComponent> uid, FixedPoint2 amount)
    {
        if (uid.Comp.Balance + amount < 0)
            return false;
        var oldBalance = uid.Comp.Balance;

        var newBalance = uid.Comp.Balance + amount;

        var ev = new BankChangeBalanceEvent()
        {
            OldBalance = oldBalance,
            Balance = newBalance
        };
        RaiseLocalEvent(uid, ev, true);

        return ev.Handled;
    }
    public bool TrySetBalance(Entity<BankAccountComponent> uid, FixedPoint2 amount)
    {

        if (uid.Comp.Balance + amount < 0)
            return false;
        var oldBalance = uid.Comp.Balance;

        var ev = new BankChangeBalanceEvent()
        {
            OldBalance = oldBalance,
            Balance = amount
        };
        RaiseLocalEvent(uid, ev, true);

        return ev.Handled;
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        Clear();
    }

    public bool TryGetBankAccount(EntityUid? bankAccountOwner, [NotNullWhen(true)] out Entity<BankAccountComponent>? bankAccount)
    {
        if (TryComp<BankAccountComponent>(bankAccountOwner, out var entBankAccount))
        {
            bankAccount = (bankAccountOwner.Value, entBankAccount);
            return true;
        }

        bankAccount = null;
        return false;
    }

    /// <summary>
    /// ТОЛЬКО ИЗ РУЧНОГО НАБОРА ПОЛЬЗОВАТЕЛЯ!!!
    /// </summary>
    /// <param name="bankAccountNumber"></param>
    /// <param name="bankAccount"></param>
    /// <returns></returns>
    public bool TryGetBankAccount(string? bankAccountNumber, [NotNullWhen(true)] out Entity<BankAccountComponent>? bankAccount)
    {
        bankAccount = null;
        if (bankAccountNumber == null)
        {
            return false;
        }

        if (!ActiveBankAccounts.TryGetValue(bankAccountNumber, out var bankInfo))
        {
            return false;
        }
        DebugTools.Assert(bankAccountNumber == bankInfo.Comp.AccountNumber);
        bankAccount = bankInfo;
        return true;
    }

    public Entity<BankAccountComponent>? CreateNewBankAccount(EntityUid idCardId, string? bankAccountNumber = null, bool isInfinite = false)
    {

        if(bankAccountNumber == null)
        {
            int number;
            do
            {
                number = _robustRandom.Next(111111, 999999);
            } while (ActiveBankAccounts.ContainsKey(number.ToString()));
            bankAccountNumber = number.ToString();
        }
        var bankAccountPin = GenerateBankAccountPin();
        var bankAccount = EnsureComp<BankAccountComponent>(idCardId);
        bankAccount.AccountNumber = bankAccountNumber;
        bankAccount.AccountPin = bankAccountPin;
        bankAccount.IsInfinite = isInfinite;
        Dirty(idCardId,bankAccount);
        return ActiveBankAccounts.TryAdd(bankAccountNumber, (idCardId,bankAccount))
            ? (idCardId, bankAccount)
            : null;
    }
    private string GenerateBankAccountPin()
    {
        var pin = string.Empty;
        for (var i = 0; i < 4; i++)
        {
            pin += _robustRandom.Next(0, 9).ToString();
        }
        return pin;
    }

    public bool TryWithdrawFromBankAccount(Entity<BankAccountComponent>? bankAccount, KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency)
    {
        if (bankAccount == null)
        {
            return false;
        }
        return TryWithdrawFromBankAccount(bankAccount.Value, currency);
    }

    public bool TryWithdrawFromBankAccount(EntityUid bankAccount,
        KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency, BankAccountComponent? bankAccountComponent)
    {
        return Resolve(bankAccount, ref bankAccountComponent, false) && TryWithdrawFromBankAccount((bankAccount, bankAccountComponent), currency);
    }

    public bool TryWithdrawFromBankAccount(Entity<BankAccountComponent> bankAccount, KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency)
    {
        if (currency.Key != bankAccount.Comp.CurrencyType)
            return false;

        var oldBalance = bankAccount.Comp.Balance;
        var result = TryChangeBalanceBy(bankAccount, -currency.Value);
        if (result)
        {
            _adminLogger.Add(
                LogType.Transactions,
                LogImpact.Low,
                $"Account {bankAccount.Comp.AccountNumber} ({bankAccount.Comp.AccountName ?? "??"})  balance was changed by {-currency.Value}, from {oldBalance} to {bankAccount.Comp.Balance}");
        }

        return result;
    }

    public bool TryInsertToBankAccount(string? bankAccountNumber, KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency)
    {
        if (!TryGetBankAccount(bankAccountNumber, out var bankAccount))
            return false;

        if (!TryInsertToBankAccount(bankAccount, currency))
            return false;

        return true;
    }

    public bool TryInsertToBankAccount(Entity<BankAccountComponent>? bankAccount, KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency)
    {
        if (bankAccount == null)
            return false;

        return TryInsertToBankAccount(bankAccount.Value, currency);
    }

    public bool TryInsertToBankAccount(Entity<BankAccountComponent> bankAccount, KeyValuePair<ProtoId<CurrencyPrototype>, FixedPoint2> currency)
    {
        if (currency.Key != bankAccount.Comp.CurrencyType)
            return false;

        var oldBalance = bankAccount.Comp.Balance;
        var result = TryChangeBalanceBy(bankAccount, currency.Value);
        if (result)
        {
            _adminLogger.Add(
                LogType.Transactions,
                LogImpact.Low,
                $"Account {bankAccount.Comp.AccountNumber} ({bankAccount.Comp.AccountName ?? "??"})  balance was changed by {-currency.Value}, from {oldBalance} to {bankAccount.Comp.Balance}");
        }

        return result;
    }

    public bool TryTransferFromToBankAccount(Entity<BankAccountComponent>? bankAccountFromNumber,
        Entity<BankAccountComponent>? bankAccountToNumber, FixedPoint2 amount)
    {
        if (bankAccountFromNumber == null || bankAccountToNumber == null)
            return false;
        return TryTransferFromToBankAccount(bankAccountFromNumber.Value, bankAccountToNumber.Value, amount);
    }

    public bool TryTransferFromToBankAccount(Entity<BankAccountComponent> bankAccountFrom, Entity<BankAccountComponent> bankAccountTo, FixedPoint2 amount)
    {
        if (bankAccountFrom.Comp.CurrencyType != bankAccountTo.Comp.CurrencyType)
            return false;
        if (!TryChangeBalanceBy(bankAccountFrom, -amount))
            return false;
        var result = TryChangeBalanceBy(bankAccountTo,amount);
        if (result)
        {
            _adminLogger.Add(
                LogType.Transactions,
                LogImpact.Low,
                $"Account {bankAccountFrom.Comp.AccountNumber} ({bankAccountFrom.Comp.AccountName ?? "??"})  transfered {amount} to account {bankAccountTo.Comp.AccountNumber} ({bankAccountTo.Comp.AccountName ?? "??"})");
        }
        else
        {
            TryChangeBalanceBy(bankAccountFrom, amount); // rollback
        }

        return result;
    }

    public void TryGenerateStartingBalance(BankAccountComponent bankAccount, JobPrototype jobPrototype)
    {
        if (jobPrototype.MaxBankBalance <= 0)
            return;

        var newBalance = FixedPoint2.New(_robustRandom.Next(jobPrototype.MinBankBalance, jobPrototype.MaxBankBalance));
        bankAccount.SetBalance(newBalance);
    }
    public void Clear()
    {
        ActiveBankAccounts.Clear();
    }
}
