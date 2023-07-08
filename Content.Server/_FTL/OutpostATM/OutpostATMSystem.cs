using Content.Server.Administration;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._FTL.OutpostATM;

/// <summary>
/// This handles the withdrawal and deposit of credits into the cargo system
/// </summary>
public sealed class OutpostATMSystem : EntitySystem
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OutpostATMComponent,GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CreditComponent,AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<OutpostATMComponent, PowerChangedEvent>(HandlePowerChange);
        SubscribeLocalEvent<OutpostATMComponent, ExaminedEvent>(OnExaminedEvent);
    }

    private void OnExaminedEvent(EntityUid uid, OutpostATMComponent component, ExaminedEvent args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;
        args.PushMarkup(Loc.GetString("outpost-atm-component-examine-message", ("credits", bankAccountComponent.Balance)));
    }

    private void HandlePowerChange(EntityUid uid, OutpostATMComponent component, ref PowerChangedEvent args)
    {
        component.Enabled = args.Powered;
    }

    private void OnAfterInteract(EntityUid uid, CreditComponent component, AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !TryComp<OutpostATMComponent>(args.Target, out var atmComponent))
            return;
        if (!TryComp<StackComponent>(uid, out var stackComponent))
            return;
        var xform = Transform(args.Target.Value);
        if (!atmComponent.Enabled || xform.GridUid == null)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;
        _cargoSystem.UpdateBankAccount(station.Value, bankAccountComponent, stackComponent.Count);
        _popupSystem.PopupCoordinates(Loc.GetString("outpost-atm-component-popup-successful-deposit", ("credits", stackComponent.Count.ToString())), xform.Coordinates);
        QueueDel(uid);
    }

    private void OnGetVerbs(EntityUid uid, OutpostATMComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;
        if (!actor.PlayerSession.AttachedEntity.HasValue)
            return;
        var xform = Transform(uid);
        if (!component.Enabled || !xform.Anchored || xform.GridUid == null)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("outpost-atm-component-verb-withdraw"),
            Act = () =>
            {
                _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("outpost-atm-component-verb-withdraw"), "Amount",
                (int amount) =>
                {
                    amount = Math.Abs(amount);
                    if (bankAccountComponent.Balance >= amount)
                    {
                        // todo spawn the money
                        _cargoSystem.UpdateBankAccount(station.Value, bankAccountComponent, -amount);
                        _popupSystem.PopupEntity(Loc.GetString("outpost-atm-component-popup-successful-withdrawal", ("credits", amount)), uid);

                        var money = _stackSystem.Spawn(amount, _prototypeManager.Index<StackPrototype>("Credit"), Transform(args.User).Coordinates);
                        if (TryComp<HandsComponent>(actor.PlayerSession.AttachedEntity.Value, out var handsComponent) && handsComponent.ActiveHand != null)
                            _handsSystem.DoPickup(actor.PlayerSession.AttachedEntity.Value, handsComponent.ActiveHand, money);

                        return;
                    }
                    _popupSystem.PopupEntity(Loc.GetString("outpost-atm-component-popup-insufficient-balance"), uid);
                });
            }
        });
    }
}
