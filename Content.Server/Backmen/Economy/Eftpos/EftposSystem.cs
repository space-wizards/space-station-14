// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Backmen.Economy;
using Content.Shared.Backmen.Economy.Eftpos;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Backmen.Economy.Eftpos;

public sealed class EftposSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly BankManagerSystem _bankManagerSystem = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdCardComponent, AfterInteractEvent>(OnAfterInteract);
        //SubscribeLocalEvent<EftposComponent, ComponentStartup>((Entity<EftposComponent> ent, ref ComponentStartup _) => UpdateComponentUserInterface(ent));
        SubscribeLocalEvent<EftposComponent, AfterActivatableUIOpenEvent>(OnInteract);
        SubscribeLocalEvent<EftposComponent, EftposChangeValueMessage>(OnChangeValue);
        SubscribeLocalEvent<EftposComponent, EftposChangeLinkedAccountNumberMessage>(OnChangeLinkedAccountNumber);
        SubscribeLocalEvent<EftposComponent, EftposSwipeCardMessage>(OnSwipeCard);
        SubscribeLocalEvent<EftposComponent, EftposLockMessage>(OnLock);
    }

    private void OnInteract(Entity<EftposComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateComponentUserInterface(ent, args.Actor);
    }

    private void UpdateComponentUserInterface(Entity<EftposComponent> uid, EntityUid? player = null)
    {
        if(!_uiSystem.HasUi(uid.Owner, EftposUiKey.Key))
            return;

        var currencyType = uid.Comp.LinkedAccount?.Comp.CurrencyType;
        var accountNumber = uid.Comp.LinkedAccount?.Comp.AccountNumber;
        var accountName = uid.Comp.LinkedAccount?.Comp.AccountName;


        string? currSymbol = null;
        if (currencyType != null && _prototypeManager.TryIndex(currencyType, out CurrencyPrototype? p))
            currSymbol = Loc.GetString(p.CurrencySymbol);
        var newState = new SharedEftposComponent.EftposBoundUserInterfaceState(
            uid.Comp.Value,
            accountNumber,
            accountName,
            uid.Comp.LockedBy != null,
            currSymbol);

        _uiSystem.SetUiState(uid.Owner, EftposUiKey.Key, newState);
    }
    private void OnChangeValue(Entity<EftposComponent> uid, ref EftposChangeValueMessage msg)
    {
        if (uid.Comp.LockedBy != null)
        {
            Deny(uid);
            return;
        }
        if (msg.Actor is not { Valid: true } mob)
            return;
        uid.Comp.Value =
            msg.Value != null
            ? FixedPoint2.Clamp((FixedPoint2) msg.Value, 0, FixedPoint2.MaxValue)
            : null;

        UpdateComponentUserInterface(uid, mob);
    }
    private void OnChangeLinkedAccountNumber(Entity<EftposComponent> uid, ref EftposChangeLinkedAccountNumberMessage msg)
    {
        if (uid.Comp.LockedBy != null || !uid.Comp.CanChangeAccountNumber)
        {
            Deny(uid);
            return;
        }

        if (msg.Actor is not { Valid: true } mob)
            return;

        if (msg.LinkedAccountNumber == null)
        {
            uid.Comp.LinkedAccount = null;
            UpdateComponentUserInterface(uid);
            Apply(uid);
            return;
        }

        Entity<BankAccountComponent>? linkedAccount;

        if (msg.LinkedAccountNumber == "auto")
        {
            if (!_idCardSystem.TryFindIdCard(msg.Actor, out var idCardComponent))
            {
                Deny(uid);
                return;
            }

            if (!_bankManagerSystem.TryGetBankAccount(idCardComponent.Owner, out linkedAccount))
            {
                Deny(uid);
                return;
            }
        }
        else if (!_bankManagerSystem.TryGetBankAccount(msg.LinkedAccountNumber, out linkedAccount))
        {
            Deny(uid);
            return;
        }

        uid.Comp.LinkedAccount = linkedAccount;

        Apply(uid);
        UpdateComponentUserInterface(uid);
    }
    private void OnSwipeCard(Entity<EftposComponent> uid, ref EftposSwipeCardMessage msg)
    {
        if (msg.Actor is not { Valid: true } buyer)
            return;
        if (!_idCardSystem.TryFindIdCard(buyer, out var idCardComponent))
        {
            Deny(uid);
            return;
        }
        TryCompleteTransaction(uid, idCardComponent);
    }
    private void OnAfterInteract(Entity<IdCardComponent> uid, ref AfterInteractEvent args)
    {
        if (!TryComp<EftposComponent>(args.Target, out var eftpos))
            return;
        TryCompleteTransaction((args.Target.Value, eftpos), uid);
    }
    private void TryCompleteTransaction(Entity<EftposComponent> terminal, Entity<IdCardComponent> idCardComponent)
    {
        if (idCardComponent.Owner == terminal.Comp.LockedBy)
        {
            terminal.Comp.LockedBy = null;
            UpdateComponentUserInterface(terminal);
            Apply(terminal);
            return;
        }

        if (terminal.Comp.Value == null)
            return;

        if (!_bankManagerSystem.TryGetBankAccount(idCardComponent.Owner, out var payerAccount))
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-deny-nomoney"),terminal.Owner, PopupType.LargeCaution);
            Deny(terminal);
            return;
        }

        if (!_bankManagerSystem.TryTransferFromToBankAccount(
                payerAccount,
                terminal.Comp.LinkedAccount,
                (FixedPoint2) terminal.Comp.Value))
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-deny-nomoney"),terminal.Owner, PopupType.LargeCaution);
            Deny(terminal);
            return;
        }
        _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-apply-done"),terminal.Owner, PopupType.Large);
        Apply(terminal);
        UpdateComponentUserInterface(terminal);
    }

    private void OnLock(Entity<EftposComponent> uid, ref EftposLockMessage msg)
    {
        if (msg.Actor is not { Valid: true } buyer)
            return;
        if (uid.Comp.LockedBy != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-deny-lock-already"), uid, PopupType.SmallCaution);
            Deny(uid);
            return;
        }
        if (uid.Comp.LinkedAccount == null || uid.Comp.Value == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-deny-lock-invalid"), uid,
                PopupType.SmallCaution);
            Deny(uid);
            return;
        }
        if (!_idCardSystem.TryFindIdCard(buyer, out var idCardComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-deny-lock-noidcard"),uid, PopupType.SmallCaution);
            Deny(uid);
            return;
        }
        uid.Comp.LockedBy = idCardComponent.Owner;
        _popupSystem.PopupEntity(Loc.GetString("eftpos-ui-popup-lock"), uid, PopupType.Small);
        Apply(uid);
        UpdateComponentUserInterface(uid);
    }

    private void Deny(Entity<EftposComponent> component)
    {
        _audioSystem.PlayPvs(component.Comp.SoundDeny, component.Owner, AudioParams.Default.WithVolume(-2f));
    }
    private void Apply(Entity<EftposComponent> component)
    {
        _audioSystem.PlayPvs(component.Comp.SoundApply, component.Owner, AudioParams.Default.WithVolume(-2f));
    }
}
