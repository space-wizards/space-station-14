using Content.Server.Mind.Components;
using Content.Server.TraitorDeathMatch.Components;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.TraitorDeathMatch;

public sealed class TraitorDeathMatchRedemptionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private const string TcCurrencyPrototype = "Telecrystal";

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
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-mind-message"))), uid, args.User);
            return;
        }

        var userMind = userMindComponent.Mind;
        if (userMind == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-user-mind-message"))), uid, args.User);
            return;
        }

        if (!EntityManager.TryGetComponent<StoreComponent>(args.Used, out var victimUplink))
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-message"))), uid, args.User);
            return;
        }

        if (!EntityManager.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent>(args.Used,
                out var victimPDAuid))
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-owner-message"))), uid, args.User);
            return;
        }

        if (victimPDAuid.UserId == userMind.UserId)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-pda-different-user-message"))), uid, args.User);
            return;
        }

        StoreComponent? userUplink = null;

        if (_inventory.TryGetSlotEntity(args.User, "id", out var pdaUid) &&
            EntityManager.TryGetComponent<StoreComponent>(pdaUid, out var userUplinkComponent))
            userUplink = userUplinkComponent;

        if (userUplink == null)
        {
            _popup.PopupEntity(Loc.GetString(
                "traitor-death-match-redemption-component-interact-using-main-message",
                ("secondMessage",
                    Loc.GetString(
                        "traitor-death-match-redemption-component-interact-using-no-pda-in-pocket-message"))), uid, args.User);
            return;
        }


        // We have finally determined both PDA components. FINALLY.

        // 4 is the per-PDA bonus amount
        var transferAmount = _uplink.GetTCBalance(victimUplink) + 4;
        victimUplink.Balance.Clear();
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>() { {"Telecrystal", FixedPoint2.New(transferAmount)}}, userUplink);

        EntityManager.DeleteEntity(victimUplink.Owner);

        _popup.PopupEntity(Loc.GetString("traitor-death-match-redemption-component-interact-using-success-message",
                ("tcAmount", transferAmount)), uid, args.User);

        args.Handled = true;
    }
}
