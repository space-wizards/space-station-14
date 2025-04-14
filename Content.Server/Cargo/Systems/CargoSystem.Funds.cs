using System.Linq;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.UserInterface;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    public void InitializeFunds()
    {
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleWithdrawFundsMessage>(OnWithdrawFunds);
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleToggleLimitMessage>(OnToggleLimit);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, SetFundingAllocationBuiMessage>(OnSetFundingAllocation);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, BeforeActivatableUIOpenEvent>(OnFundAllocationBuiOpen);
    }

    private void OnWithdrawFunds(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleWithdrawFundsMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        if (args.Account == ent.Comp.Account ||
            args.Amount <= 0 ||
            args.Amount > GetBalanceFromAccount((station, bank), ent.Comp.Account) * ent.Comp.TransferLimit)
            return;

        if (_timing.CurTime < ent.Comp.NextAccountActionTime)
            return;

        if (!_accessReaderSystem.IsAllowed(args.Actor, ent))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        ent.Comp.NextAccountActionTime = _timing.CurTime + ent.Comp.AccountActionDelay;
        Dirty(ent);
        UpdateBankAccount((station, bank), -args.Amount, CreateAccountDistribution(ent.Comp.Account, bank));
        _audio.PlayPvs(ApproveSound, ent);

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, args.Actor);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);

        var ourAccount = _protoMan.Index(ent.Comp.Account);
        if (args.Account == null)
        {
            var stackPrototype = _protoMan.Index(ent.Comp.CashType);
            _stack.Spawn(args.Amount, stackPrototype, Transform(ent).Coordinates);

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-withdraw-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
        else
        {
            var otherAccount = _protoMan.Index(args.Account.Value);
            UpdateBankAccount((station, bank), args.Amount, CreateAccountDistribution(args.Account.Value, bank));

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-transfer-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)),
                    ("name2", Loc.GetString(otherAccount.Name)),
                    ("code2", Loc.GetString(otherAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
                _radio.SendRadioMessage(ent, msg, otherAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
    }

    private void OnToggleLimit(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleToggleLimitMessage args)
    {
        if (!_accessReaderSystem.FindAccessTags(args.Actor).Intersect(ent.Comp.RemoveLimitAccess).Any())
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        _audio.PlayPvs(ent.Comp.ToggleLimitSound, ent);
        ent.Comp.TransferUnbounded = !ent.Comp.TransferUnbounded;
        Dirty(ent);
    }


    private void OnSetFundingAllocation(Entity<FundingAllocationConsoleComponent> ent, ref SetFundingAllocationBuiMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        if (args.Percents.Count != bank.RevenueDistribution.Count)
            return;

        var differs = false;
        foreach (var (account, percent) in args.Percents)
        {
            if (percent != (int) Math.Round(bank.RevenueDistribution[account] * 100))
            {
                differs = true;
                break;
            }
        }

        if (!differs)
            return;

        if (args.Percents.Values.Sum() != 100)
            return;

        bank.RevenueDistribution.Clear();
        foreach (var (account, percent )in args.Percents)
        {
            bank.RevenueDistribution.Add(account, percent / 100.0);
        }
        Dirty(station, bank);

        _audio.PlayPvs(ent.Comp.SetDistributionSound, ent);
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set station {ToPrettyString(station)} fund distribution: {string.Join(',', bank.RevenueDistribution.Select(p => $"{p.Key}: {p.Value}").ToList())}");
    }

    private void OnFundAllocationBuiOpen(Entity<FundingAllocationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (_station.GetOwningStation(ent) is { } station)
            _uiSystem.SetUiState(ent.Owner, FundingAllocationConsoleUiKey.Key, new FundingAllocationConsoleBuiState(GetNetEntity(station)));
    }
}
