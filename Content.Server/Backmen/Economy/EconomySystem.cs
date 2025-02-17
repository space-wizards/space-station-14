// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Managers;
using Content.Server.Backmen.CartridgeLoader.Cartridges;
using Content.Server.Backmen.Economy.Eftpos;
using Content.Server.Backmen.Economy.Wage;
using Content.Server.Backmen.Mind;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Backmen.Economy;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Objectives.Components;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Backmen.Economy;

public sealed class EconomySystem : EntitySystem
{
    [Dependency] private readonly BankManagerSystem _bankManagerSystem = default!;
    [Dependency] private readonly WageManagerSystem _wageManagerSystem = default!;
    [Dependency] private readonly BankCartridgeSystem _bankCartridgeSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IdCardSystem _cardSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly BankManagerSystem _bankManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    [ValidatePrototypeId<EntityPrototype>] private readonly string MindRoleBankMemory = "MindRoleBankMemory";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned
            , after: new[] { typeof(StationRecordsSystem) }
            );
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStartingEvent);
        SubscribeLocalEvent<EftposComponent, MapInitEvent>(OnFtposInit);
        SubscribeLocalEvent<MindNoteConditionComponent, ObjectiveGetProgressEvent>(OnGetBankProgress);
        SubscribeLocalEvent<MindNoteConditionComponent, ObjectiveAfterAssignEvent>(OnAfterBankAssign);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);
    }

    #region Verb

    private void GetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Adminhelp))
        {
            return;
        }

        if (TryComp<PdaComponent>(args.Target, out var pdaComponent) && pdaComponent.IdSlot.Item != null)
        {
            if (TryComp<IdCardComponent>(pdaComponent.IdSlot.Item, out var idCardComponent))
            {
                GetIdCardVerb(args, (pdaComponent.IdSlot.Item.Value, idCardComponent), actor);
            }
        }

        {
            if (TryComp<IdCardComponent>(args.Target, out var idCardComponent))
            {
                GetIdCardVerb(args, (args.Target, idCardComponent), actor);
            }
        }
    }

    private static readonly ResPath BankIcon = new("_Backmen/Objects/Tools/rimbank.rsi");
    private void GetIdCardVerb(GetVerbsEvent<Verb> args, Entity<IdCardComponent> card, ActorComponent actor)
    {
        if (!_bankManagerSystem.TryGetBankAccount(card.Owner, out var account))
        {
            Verb verb = new();
            verb.Text = Loc.GetString("prayer-verbs-bank-new");
            verb.Message = "Создать счет";
            verb.Category = VerbCategory.Tricks;
            verb.Icon = new SpriteSpecifier.Rsi(BankIcon, "icon");
            verb.Act = () =>
            {
                var bankAccount = _bankManagerSystem.CreateNewBankAccount(card);
                DebugTools.Assert(bankAccount != null);
                bankAccount.Value.Comp.AccountName = card.Comp.FullName;
                card.Comp.StoredBankAccountNumber = bankAccount.Value.Comp.AccountNumber;
                Dirty(card);
                Dirty(bankAccount.Value);
                var msg = $"Новый счет в банке №{bankAccount.Value.Comp.AccountNumber}, пин-код {bankAccount.Value.Comp.AccountPin}";
                _chatManager.ChatMessageToOne(ChatChannel.Admin, msg, msg, EntityUid.FirstUid, false, actor.PlayerSession.Channel);
            };
            verb.Impact = LogImpact.Low;
            args.Verbs.Add(verb);

            return;
        }


        {
            Verb verb = new();
            verb.Text = Loc.GetString("prayer-verbs-bank-getpin");
            verb.Message = "Получить пин код карты";
            verb.Category = VerbCategory.Tricks;
            verb.Icon = new SpriteSpecifier.Rsi(BankIcon, "icon");
            verb.Act = () =>
            {
                var msg = $"Cчет в банке: №{account.Value.Comp.AccountNumber}, пин-код {account.Value.Comp.AccountPin}, баланс {account.Value.Comp.Balance}";
                _chatManager.ChatMessageToOne(ChatChannel.Admin, msg, msg, EntityUid.FirstUid, false, actor.PlayerSession.Channel);
            };
            verb.Impact = LogImpact.Low;
            args.Verbs.Add(verb);
        }
    }

    #endregion


    private void OnGetBankProgress(EntityUid _, MindNoteConditionComponent component,
        ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1;
    }

    private void OnAfterBankAssign(EntityUid uid, MindNoteConditionComponent component,
        ref ObjectiveAfterAssignEvent args)
    {
        if (!_roleSystem.MindHasRole<BankMemoryComponent>(args.MindId, out var bankMemory) ||
            !TryComp<BankMemoryComponent>(bankMemory, out var bankMemoryComp))
        {
            UpdateNote(uid);
            return;
        }

        if (!TryComp<BankAccountComponent>(bankMemoryComp.BankAccount, out var bankAccount))
        {
            UpdateNote(uid);
            return;
        }

        UpdateNote(uid, bankAccount);
    }

    private void UpdateNote(EntityUid uid, BankAccountComponent? bank = null)
    {
        _metaDataSystem.SetEntityName(uid, Loc.GetString("character-info-memories-placeholder-text"));
        _metaDataSystem.SetEntityDescription(uid, bank != null
            ? Loc.GetString("memory-account-number", ("value", bank!.AccountNumber)) + "\n" +
              Loc.GetString("memory-account-pin", ("value", bank!.AccountPin))
            : "");
        DirtyEntity(uid);
    }

    private void OnFtposInit(EntityUid uid, EftposComponent component, MapInitEvent args)
    {
        if (component.PresetAccountNumber == null)
        {
            return;
        }

        if (!_bankManagerSystem.TryGetBankAccount(component.PresetAccountNumber, out var account))
        {
            var dummy = Spawn("CaptainIDCard");
            _metaDataSystem.SetEntityName(dummy, $"Bank: {component.PresetAccountNumber}");
            account = _bankManagerSystem.CreateNewBankAccount(dummy, component.PresetAccountNumber);
            DebugTools.Assert(account != null);
            account.Value.Comp.AccountName = component.PresetAccountName ?? component.PresetAccountNumber;
        }

        component.LinkedAccount = account;
        Dirty(uid, component);
    }

    #region EventHandle

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        AddPlayerBank(ev.Mob);
    }

    private void OnRoundStartingEvent(RoundStartingEvent ev)
    {
        foreach (var department in _prototype.EnumeratePrototypes<DepartmentPrototype>())
        {
            var dummy = Spawn("CaptainIDCard");
            _metaDataSystem.SetEntityName(dummy, "Bank: " + department.AccountNumber);
            var bankAccount = _bankManagerSystem.CreateNewBankAccount(dummy, department.AccountNumber, true);
            if (bankAccount == null)
                continue;
            bankAccount.Value.Comp.AccountName = department.ID;
            bankAccount.Value.Comp.Balance = 100_000;
        }
    }

    #endregion

    #region PublicApi

    [PublicAPI]
    public bool TryStoreNewBankAccount(EntityUid player, Entity<IdCardComponent> idCardId,
        [NotNullWhen(true)] out Entity<BankAccountComponent>? bankAccount)
    {
        bankAccount = null;

        bankAccount = _bankManager.CreateNewBankAccount(idCardId);
        if (bankAccount == null)
            return false;
        var account = bankAccount.Value;
        idCardId.Comp.StoredBankAccountNumber = account.Comp.AccountNumber;
        account.Comp.AccountName = idCardId.Comp.FullName;
        if (string.IsNullOrEmpty(account.Comp.AccountName))
        {
            account.Comp.AccountName = MetaData(player).EntityName;
        }

        Dirty(bankAccount.Value);
        return true;
    }

    private void AttachPdaBank(EntityUid player, BankAccountComponent bankAccount)
    {
        if (!_inventorySystem.TryGetSlotEntity(player, "id", out var idUid))
            return;

        if (!EntityManager.TryGetComponent(idUid, out CartridgeLoaderComponent? cartrdigeLoaderComponent))
            return;

        foreach (var uid in cartrdigeLoaderComponent.BackgroundPrograms)
        {
            if (!TryComp<BankCartridgeComponent>(uid, out var bankCartrdigeComponent))
                continue;

            if (bankCartrdigeComponent.LinkedBankAccount == null)
            {
                _bankCartridgeSystem.LinkBankAccountToCartridge(uid, bankAccount, bankCartrdigeComponent);
            }
            else if (bankCartrdigeComponent.LinkedBankAccount.AccountNumber != bankAccount.AccountNumber)
            {
                _bankCartridgeSystem.UnlinkBankAccountFromCartridge(uid, bankCartrdigeComponent.LinkedBankAccount,
                    bankCartrdigeComponent);
                _bankCartridgeSystem.LinkBankAccountToCartridge(uid, bankAccount, bankCartrdigeComponent);
            }
            // else: do nothing
        }
    }

    [PublicAPI]
    public Entity<BankAccountComponent>? AddPlayerBank(EntityUid player,
        Entity<BankAccountComponent>? bankAccount = null, bool AttachWage = true)
    {
        if (!_cardSystem.TryFindIdCard(player, out var idCardComponent))
            return null;

        if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            return null;
        }

        if (bankAccount == null)
        {
            if (!TryStoreNewBankAccount(player, idCardComponent, out bankAccount))
            {
                return null;
            }

            if (AttachWage && !_roleSystem.MindHasRole<JobRoleComponent>(mindId))
            {
                AttachWage = false;
            }

            if (_roleSystem.MindHasRole<JobRoleComponent>(mindId, out var jobComponent) && jobComponent?.Comp1.JobPrototype != null &&
                _prototype.TryIndex(jobComponent?.Comp1.JobPrototype, out var jobPrototype))
            {
                _bankManagerSystem.TryGenerateStartingBalance(bankAccount, jobPrototype);

                if (AttachWage)
                {
                    _wageManagerSystem.TryAddAccountToWagePayoutList(bankAccount.Value, jobPrototype);
                }
            }
        }

        if (_roleSystem.MindHasRole<BankMemoryComponent>(mindId))
        {
            _roleSystem.MindRemoveRole<BankMemoryComponent>(mindId);
        }

        _roleSystem.MindAddRole(mindId, MindRoleBankMemory);
        _roleSystem.MindHasRole<BankMemoryComponent>(mindId, out var bankRoleComp);
        EnsureComp<BankMemoryComponent>(bankRoleComp!.Value).BankAccount = bankAccount;

        var needAdd = true;
        foreach (var condition in mind.AllObjectives.Where(HasComp<MindNoteConditionComponent>))
        {
            var md = Comp<MindNoteConditionComponent>(condition);
            Dirty(condition, md);
            needAdd = false;
        }

        if (needAdd)
        {
            _mindSystem.TryAddObjective(mindId, mind, BankNoteCondition);
        }

        return bankAccount;
    }

    #endregion

    [ValidatePrototypeId<EntityPrototype>] private const string BankNoteCondition = "BankNote";
}
