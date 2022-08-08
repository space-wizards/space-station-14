using Content.Server.Mind.Components;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.TraitorDeathMatch.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.TraitorDeathMatch;

public sealed class TraitorDeathMatchRedemptionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UplinkAccountsSystem _uplink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorDeathMatchRedemptionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, TraitorDeathMatchRedemptionComponent component, InteractUsingEvent args)
    {
        if (!EntityManager.TryGetComponent<MindComponent>(args.User, out var userMindComponent))
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-mind-message"))), uid, Filter.Entities(args.User));
            return;
        }

        var userMind = userMindComponent.Mind;
        if (userMind == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-user-mind-message"))), uid, Filter.Entities(args.User));
            return;
        }

        if (!EntityManager.TryGetComponent<UplinkComponent>(args.Used, out var victimUplink))
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-message"))), uid, Filter.Entities(args.User));
            return;
        }

        if (!EntityManager.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent>(args.Used,
                out var victimPDAuid))
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-owner-message"))), uid, Filter.Entities(args.User));
            return;
        }

        if (victimPDAuid.UserId == userMind.UserId)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-pda-different-user-message"))), uid, Filter.Entities(args.User));
            return;
        }

        UplinkComponent? userUplink = null;

        if (_inventory.TryGetSlotEntity(args.User, "id", out var pdaUid) &&
            EntityManager.TryGetComponent<UplinkComponent>(pdaUid, out var userUplinkComponent))
            userUplink = userUplinkComponent;

        if (userUplink == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-no-pda-in-pocket-message"))), uid, Filter.Entities(args.User));
            return;
        }

        // We have finally determined both PDA components. FINALLY.

        var userAccount = userUplink.UplinkAccount;
        var victimAccount = victimUplink.UplinkAccount;

        if (userAccount == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-user-no-uplink-account-message"))), uid, Filter.Entities(args.User));
            return;
        }

        if (victimAccount == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-victim-no-uplink-account-message"))), uid, Filter.Entities(args.User));
            return;
        }

        // 4 is the per-PDA bonus amount.
        var transferAmount = victimAccount.Balance + 4;
        _uplink.SetBalance(victimAccount, 0);
        _uplink.AddToBalance(userAccount, transferAmount);

        EntityManager.DeleteEntity(victimUplink.Owner);

        _popup.PopupEntity(Loc.GetString("traitor-death-match-redemption-component-interact-using-success-message",
                ("tcAmount", transferAmount)), uid, Filter.Entities(args.User));

        args.Handled = true;
    }
}
