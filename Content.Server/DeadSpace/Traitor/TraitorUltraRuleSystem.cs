// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Backmen.Economy;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Backmen.Economy;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Corvax.TTS;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Traitor;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gibbing;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.Traitor;

public sealed class TraitorUltraRuleSystem : GameRuleSystem<TraitorUltraRuleComponent>
{
    private const int SourceParentSearchDepth = 8;
    private const string DefaultTraitorUltraRule = "TraitorUltra";
    private const string StandardUplinkImplant = "UplinkImplant";

    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly BankManagerSystem _bankManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ISharedChatManager _chatManager = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedObjectivesSystem _sharedObjectives = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjectives = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private readonly List<TraitorUltraDelayedAction> _delayedActions = new();
    private readonly Dictionary<EntityUid, TraitorUltraOfferEui> _openUpgradeOfferEuis = new();
    private readonly Dictionary<EntityUid, TraitorUltraExtraObjectiveOfferEui> _openExtraObjectiveOfferEuis = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorUltraRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagSelected, after: [typeof(TraitorRuleSystem)]);
        SubscribeLocalEvent<MindComponent, BeforeMindGotRemovedEvent>(OnBeforeMindRemoved);
        SubscribeLocalEvent<MobStateChangedEvent>(OnCandidateMobStateChanged, before: [typeof(SubdermalImplantSystem)]);
        SubscribeLocalEvent<TraitorUltraOpenContractActionEvent>(OnOpenContractAction);
        SubscribeLocalEvent<TraitorUltraOpenExtraObjectiveOfferActionEvent>(OnOpenExtraObjectiveOfferAction);
        SubscribeLocalEvent<TraitorUltraBountyTargetComponent, DamageChangedEvent>(OnBountyDamageChanged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<TraitorUltraBountyTargetComponent, MobStateChangedEvent>(OnBountyMobStateChanged, before: [typeof(SubdermalImplantSystem)]);
        SubscribeLocalEvent<TraitorUltraBountyTargetComponent, GibbedBeforeDeletionEvent>(OnBountyGibbedBeforeDeletion);
        SubscribeLocalEvent<TraitorUltraBountyTargetComponent, EntityTerminatingEvent>(OnBountyTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _delayedActions.Clear();
        CloseAllUpgradeOfferEuis();
        CloseAllExtraObjectiveOfferEuis();
    }

    private void OnBeforeMindRemoved(EntityUid mindId, MindComponent mind, BeforeMindGotRemovedEvent args)
    {
        SnapshotAndInterruptCandidate(mindId, mind, args.Container.Owner);
    }

    private void OnCandidateMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead ||
            !_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            return;
        }

        SnapshotAndInterruptCandidate(mindId, mind, args.Target);
    }

    private void SnapshotAndInterruptCandidate(EntityUid mindId, MindComponent mind, EntityUid body)
    {
        var query = EntityQueryEnumerator<TraitorUltraRuleComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (!component.Minds.TryGetValue(mindId, out var state) ||
                state.EligibleBody != body ||
                !IsPendingUpgradeStage(state.Stage))
            {
                continue;
            }

            InitialObjectivesCompleted(mindId, mind, component);
            InterruptUpgradeOffer(mindId, state);
        }
    }

    private static bool IsPendingUpgradeStage(TraitorUltraStage stage)
    {
        return stage is TraitorUltraStage.Initial or
            TraitorUltraStage.CompletionPopupSent or
            TraitorUltraStage.OfferOpen;
    }

    private void InterruptUpgradeOffer(EntityUid mindId, TraitorUltraMindState state)
    {
        CloseUpgradeOfferEui(mindId);
        RemoveUpgradeOfferAction(state);
        state.NextEventTime = TimeSpan.Zero;

        if (state.Stage is TraitorUltraStage.CompletionPopupSent or TraitorUltraStage.OfferOpen)
            state.Stage = TraitorUltraStage.Initial;
    }

    private void OnAfterAntagSelected(Entity<TraitorUltraRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        EnsureInitialTraitorUltraSetup(ent, args.EntityUid, logMissingMind: true);
    }

    public void MakeAdminTraitorUltra(
        Entity<TraitorUltraRuleComponent, AntagSelectionComponent> rule,
        ICommonSession target,
        AntagSelectionDefinition definition)
    {
        var alreadyTraitor = _mind.TryGetMind(target, out var mindId, out _) &&
                             _roles.MindHasRole<TraitorRoleComponent>(mindId);

        if (!alreadyTraitor)
            _antag.MakeAntag((rule.Owner, rule.Comp2), target, definition);

        if (target.AttachedEntity is { } attached)
            EnsureInitialTraitorUltraSetup((rule.Owner, rule.Comp1), attached, logMissingMind: true);

        if (!_mind.TryGetMind(target, out mindId, out var mind))
            return;

        TrackAdminTraitorUltraMind(rule, target, mindId, mind);
    }

    public bool MakeAdminTraitorUltra(ICommonSession target, bool announceBounty)
    {
        var rule = GetOrCreateAdminTraitorUltraRule();

        if (!_mind.TryGetMind(target, out var mindId, out var mind))
        {
            Log.Error($"Failed to make admin TraitorUltra for {target.Name}: no mind was attached.");
            return false;
        }

        if (_roles.MindHasRole<TraitorUltraRoleComponent>(mindId))
            return false;

        var alreadyTraitor = _roles.MindHasRole<TraitorRoleComponent>(mindId);

        var state = EnsureTraitorUltraState(rule.Owner, rule.Comp1, mindId);

        if (mind.OwnedEntity is not { } body ||
            TerminatingOrDeleted(body) ||
            !TryComp<MobStateComponent>(body, out var mobState) ||
            mobState.CurrentState == MobState.Dead)
        {
            Log.Error($"Failed to make admin TraitorUltra for {target.Name}: owned entity is dead or missing.");
            return false;
        }

        // A direct admin promotion is the sole exception: it deliberately binds the contract to the current live body.
        state.EligibleBody = body;

        if (!alreadyTraitor &&
            !PrepareAdminDirectUltraTraitor(rule, target, mindId, mind, state))
        {
            return false;
        }

        TrackAdminTraitorUltraMind(rule, target, mindId, mind);
        return UpgradeTraitor(rule.Owner, rule.Comp1, mindId, mind, state, announceBounty, false);
    }

    private bool PrepareAdminDirectUltraTraitor(
        Entity<TraitorUltraRuleComponent, AntagSelectionComponent> rule,
        ICommonSession target,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state)
    {
        if (mind.OwnedEntity is not { } body || TerminatingOrDeleted(body))
        {
            Log.Error($"Failed to prepare admin TraitorUltra for {target.Name}: mind has no owned entity.");
            return false;
        }

        if (rule.Comp2.Definitions.Count > 0)
            EntityManager.AddComponents(body, rule.Comp2.Definitions[^1].Components);

        _roles.MindAddRole(mindId, rule.Comp1.TraitorMindRole, mind, silent: true);

        if (!_roles.MindHasRole<TraitorRoleComponent>(mindId))
        {
            Log.Error($"Failed to prepare admin TraitorUltra for {target.Name}: traitor role was not assigned.");
            return false;
        }

        if (!TryComp<TraitorRuleComponent>(rule.Owner, out var traitorRule) ||
            !_traitorRule.MakeTraitor(body, traitorRule))
        {
            Log.Error($"Failed to prepare admin TraitorUltra for {target.Name}: base traitor setup failed.");
            return false;
        }

        state.OriginalCorporation ??= GetOriginalCorporation(rule.Owner, mindId);

        var startingBalance = GetAdjustedStartingBalance(rule.Owner, mindId);
        if (EnsureUpgradeResources((mindId, mind), rule.Comp1, state, startingBalance))
            return true;

        Log.Error($"Failed to atomically assign the TraitorUltra implants to {ToPrettyString(mindId)}.");
        return false;
    }

    private Entity<TraitorUltraRuleComponent, AntagSelectionComponent> GetOrCreateAdminTraitorUltraRule()
    {
        var rule = _antag.ForceGetGameRuleEnt<TraitorUltraRuleComponent>(DefaultTraitorUltraRule);
        return (rule.Owner, Comp<TraitorUltraRuleComponent>(rule.Owner), rule.Comp);
    }

    private TraitorUltraMindState EnsureTraitorUltraState(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        EntityUid mindId)
    {
        if (!component.Minds.TryGetValue(mindId, out var state))
        {
            state = new TraitorUltraMindState();
            component.Minds[mindId] = state;
        }

        state.OriginalCorporation ??= GetOriginalCorporation(rule, mindId);
        return state;
    }

    private void TrackAdminTraitorUltraMind(
        Entity<TraitorUltraRuleComponent, AntagSelectionComponent> rule,
        ICommonSession target,
        EntityUid mindId,
        MindComponent mind)
    {
        _antag.AddAntagIdentifier(rule.Owner, mindId, GetMindCharacterName(mind) ?? target.Name, target);
    }

    private bool EnsureInitialTraitorUltraSetup(
        Entity<TraitorUltraRuleComponent> ent,
        EntityUid target,
        bool logMissingMind = false)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
        {
            if (logMissingMind)
                Log.Error($"TraitorUltra selected {ToPrettyString(target)} but no mind was attached.");

            return false;
        }

        if (!ent.Comp.Minds.TryGetValue(mindId, out var state))
        {
            state = new TraitorUltraMindState
            {
                EligibleBody = target,
                OriginalCorporation = GetOriginalCorporation(ent.Owner, mindId),
            };
            ent.Comp.Minds[mindId] = state;
        }
        else
        {
            state.EligibleBody ??= target;
            state.OriginalCorporation ??= GetOriginalCorporation(ent.Owner, mindId);
        }

        if (!EnsureDeathAcidifierImplant((mindId, mind), ent.Comp))
            Log.Error($"Failed to assign the TraitorUltra death-acidifier implant to {ToPrettyString(mindId)}.");

        if (state.Stage == TraitorUltraStage.Initial && !state.UltraUplinkInitialized)
        {
            if (EnsureInitialUltraUplink(ent.Owner, ent.Comp, (mindId, mind), state))
                state.UltraUplinkInitialized = true;
            else
                Log.Error($"Failed to assign the TraitorUltra uplink implant to {ToPrettyString(mindId)}.");
        }

        if (state.Stage != TraitorUltraStage.Initial)
            return true;

        if (!state.BaseObjectivesAssigned)
        {
            if (TryAssignBaseObjectives((mindId, mind), ent.Comp, state.OriginalCorporation))
                state.BaseObjectivesAssigned = true;
            else
                Log.Error($"Failed to assign a base TraitorUltra objective package to {ToPrettyString(mindId)}.");
        }

        if (!state.InitialObjectivePackageAssigned)
        {
            if (AssignInitialObjectives((mindId, mind), ent.Comp, state))
                state.InitialObjectivePackageAssigned = true;
            else
                Log.Error($"Failed to assign a medium TraitorUltra objective package to {ToPrettyString(mindId)}.");
        }

        return state.UltraUplinkInitialized && state.BaseObjectivesAssigned && state.InitialObjectivePackageAssigned;
    }

    private bool EnsureDeathAcidifierImplant(Entity<MindComponent> mind, TraitorUltraRuleComponent component)
    {
        if (mind.Comp.OwnedEntity is not { } body || TerminatingOrDeleted(body))
            return false;

        if (TryFindImplant(body, component.DeathAcidifierImplant, out _))
            return true;

        var implant = _subdermalImplant.AddImplant(body, component.DeathAcidifierImplant);
        if (implant == null)
            return false;

        _antag.EnsureRollbackTracking(body);
        _antag.TrackGrantedEntity(body, implant.Value);
        return true;
    }

    private bool EnsureUpgradeResources(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state,
        FixedPoint2? uplinkBalance = null)
    {
        if (mind.Comp.OwnedEntity is not { } body || TerminatingOrDeleted(body))
            return false;

        var hadAcidifier = TryFindImplant(body, component.DeathAcidifierImplant, out _);
        if (!EnsureDeathAcidifierImplant(mind, component))
            return false;

        if (EnsureUltraUplink(mind, component, state, uplinkBalance) != null)
        {
            state.UltraUplinkInitialized = true;
            return true;
        }

        // Roll back the only resource that may have been created before the uplink failed.
        if (!hadAcidifier && TryFindImplant(body, component.DeathAcidifierImplant, out var acidifier))
            QueueDel(acidifier);

        return false;
    }

    private bool TryFindImplant(EntityUid body, EntProtoId implantPrototype, out EntityUid implant)
    {
        implant = default;
        if (!TryComp<ImplantedComponent>(body, out var implanted))
            return false;

        foreach (var contained in implanted.ImplantContainer.ContainedEntities)
        {
            if (!TerminatingOrDeleted(contained) &&
                MetaData(contained).EntityPrototype?.ID == implantPrototype.Id)
            {
                implant = contained;
                return true;
            }
        }

        return false;
    }

    private bool EnsureInitialUltraUplink(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        Entity<MindComponent> mind,
        TraitorUltraMindState state)
    {
        var startingBalance = GetAdjustedStartingBalance(rule, mind.Owner);
        return EnsureUltraUplink(mind, component, state, startingBalance) != null;
    }

    private FixedPoint2 GetAdjustedStartingBalance(EntityUid rule, EntityUid mindId)
    {
        var startingBalance = FixedPoint2.Zero;
        if (TryComp<TraitorRuleComponent>(rule, out var traitorRule))
            startingBalance = traitorRule.StartingBalance;

        if (!_jobs.MindTryGetJob(mindId, out var prototype))
            return startingBalance;

        if (startingBalance < prototype.AntagAdvantage)
            return FixedPoint2.Zero;

        return startingBalance - prototype.AntagAdvantage;
    }

    private EntityUid? EnsureUltraUplink(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state,
        FixedPoint2? balance = null)
    {
        if (mind.Comp.OwnedEntity is not { } body || TerminatingOrDeleted(body))
            return null;

        if (TryFindUltraUplink(body, component, state, out var uplink))
        {
            EnsureUltraStoreCategories(uplink, component);

            if (balance != null)
                _uplink.SetupUplink(body, uplink, balance.Value, giveDiscounts: true);
            else if (TryComp<StoreComponent>(uplink, out var store))
                _store.UpdateUserInterface(body, uplink, store);

            return uplink;
        }

        var created = _uplink.AddImplantUplink(
            body,
            balance ?? FixedPoint2.Zero,
            component.UltraUplinkImplant,
            giveDiscounts: true);

        if (created == null)
            return null;

        _antag.EnsureRollbackTracking(body);
        _antag.TrackGrantedEntity(body, created.Value);
        state.UltraUplinkEntity = created.Value;
        EnsureUltraStoreCategories(created.Value, component);
        return created.Value;
    }

    private bool TryFindUltraUplink(
        EntityUid body,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state,
        out EntityUid uplink)
    {
        uplink = default;

        if (_uplink.FindUplinkTarget(body) is { } pdaUplink &&
            HasComp<UplinkComponent>(pdaUplink) &&
            TryComp<StoreComponent>(pdaUplink, out _))
        {
            state.UltraUplinkEntity = pdaUplink;
            uplink = pdaUplink;
            return true;
        }

        if (!TryComp<ImplantedComponent>(body, out var implanted))
            return false;

        if (state.UltraUplinkEntity is { } tracked &&
            implanted.ImplantContainer.ContainedEntities.Contains(tracked) &&
            IsReusableUplinkStore(tracked, component))
        {
            uplink = tracked;
            return true;
        }

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (!IsReusableUplinkStore(implant, component))
                continue;

            state.UltraUplinkEntity = implant;
            uplink = implant;
            return true;
        }

        return false;
    }

    private void EnsureUltraStoreCategories(EntityUid uplink, TraitorUltraRuleComponent component)
    {
        if (!TryComp<StoreComponent>(uplink, out var store))
            return;

        if (!_proto.TryIndex<EntityPrototype>(component.UltraUplinkImplant, out var uplinkPrototype) ||
            !uplinkPrototype.TryGetComponent<StoreComponent>(out var presetStore, EntityManager.ComponentFactory))
        {
            Log.Error($"Failed to find TraitorUltra store categories from {component.UltraUplinkImplant}.");
            return;
        }

        var changed = false;
        foreach (var category in presetStore.Categories)
        {
            changed |= store.Categories.Add(category);
        }

        if (changed)
            _store.RefreshAllListings(store);
    }

    private void ApplyCorporateDiscounts(StoreComponent store, TraitorUltraRuleComponent component, TraitorUltraMindState state)
    {
        foreach (var listing in store.FullListingsCatalog)
        {
            listing.RemoveCostModifier(component.CorporateDiscountModifierSourceId);
        }

        if (!HasCorporateDiscountsStage(state) ||
            string.IsNullOrWhiteSpace(state.NewCorporation) ||
            !TryFindCorporateDiscountSet(component, state.NewCorporation, out var discountSet))
        {
            return;
        }

        foreach (var discount in discountSet.Listings)
        {
            if (discount.TelecrystalDiscount <= FixedPoint2.Zero)
                continue;

            if (!TryFindStoreListing(store, discount.Listing, out var listing))
            {
                Log.Warning($"TraitorUltra corporate discount listing {discount.Listing} does not exist in the store catalog.");
                continue;
            }

            listing.AddCostModifier(
                component.CorporateDiscountModifierSourceId,
                new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
                {
                    { component.TelecrystalCurrency, -discount.TelecrystalDiscount },
                });
        }
    }

    private bool HasCorporateDiscountsStage(TraitorUltraMindState state)
    {
        return state.Stage == TraitorUltraStage.Upgraded ||
               state.Stage == TraitorUltraStage.BountyAnnounced ||
               state.Stage == TraitorUltraStage.Resolved;
    }

    private bool TryFindCorporateDiscountSet(
        TraitorUltraRuleComponent component,
        string corporation,
        out TraitorUltraCorporateDiscountSet discountSet)
    {
        foreach (var set in component.CorporateDiscounts)
        {
            if (CorporationMatches(set.Corporation, corporation))
            {
                discountSet = set;
                return true;
            }
        }

        discountSet = default!;
        return false;
    }

    private bool TryFindStoreListing(
        StoreComponent store,
        ProtoId<ListingPrototype> listingId,
        out ListingDataWithCostModifiers listing)
    {
        foreach (var item in store.FullListingsCatalog)
        {
            if (item.ID.Equals(listingId.Id, StringComparison.Ordinal))
            {
                listing = item;
                return true;
            }
        }

        listing = default!;
        return false;
    }

    private bool IsReusableUplinkStore(EntityUid entity, TraitorUltraRuleComponent component)
    {
        if (TerminatingOrDeleted(entity) ||
            !TryComp<StoreComponent>(entity, out _))
        {
            return false;
        }

        if (HasComp<UplinkComponent>(entity))
            return true;

        return MetaData(entity).EntityPrototype?.ID is { } prototype &&
               (prototype == StandardUplinkImplant || prototype == component.UltraUplinkImplant.Id);
    }

    protected override void ActiveTick(EntityUid uid, TraitorUltraRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        ProcessDelayedActions();

        if (component.NextCheck > Timing.CurTime)
            return;

        component.NextCheck = Timing.CurTime + component.CheckDelay;

        foreach (var (mindId, state) in component.Minds)
        {
            if (!TryComp<MindComponent>(mindId, out var mind))
                continue;

            if (IsPendingUpgradeStage(state.Stage) &&
                !TryGetEligibleAliveBody(mind, state, out _))
            {
                InterruptUpgradeOffer(mindId, state);
                continue;
            }

            switch (state.Stage)
            {
                case TraitorUltraStage.Initial:
                    if (!InitialObjectivesCompleted(mindId, mind, component))
                        break;

                    ShowPopup(mind, "traitor-ultra-objectives-complete-popup", PopupType.Large);
                    state.Stage = TraitorUltraStage.CompletionPopupSent;
                    state.NextEventTime = Timing.CurTime + component.UpgradeOfferDelay;
                    break;

                case TraitorUltraStage.CompletionPopupSent:
                    if (Timing.CurTime < state.NextEventTime)
                        break;

                    state.Stage = TraitorUltraStage.OfferOpen;
                    state.NextEventTime = Timing.CurTime + component.UpgradeOfferTimeout;
                    EnsureUpgradeOfferAction(component, mindId, mind, state);
                    ShowPopup(mind, "traitor-ultra-offer-ready-popup", PopupType.Large);
                    OpenUpgradeOfferEui(uid, mindId, mind);
                    break;

                case TraitorUltraStage.OfferOpen:
                    if (Timing.CurTime >= state.NextEventTime)
                    {
                        DeclineUpgradeOffer(mindId, mind, state, "traitor-ultra-offer-expired-popup");
                        break;
                    }

                    EnsureUpgradeOfferAction(component, mindId, mind, state);
                    break;

                case TraitorUltraStage.Upgraded:
                    if (state.BountyAnnouncementSuppressed)
                        break;

                    if (Timing.CurTime >= state.NextEventTime)
                        AnnounceBounty(uid, component, mindId, mind, state);
                    break;

                case TraitorUltraStage.BountyAnnounced:
                    EnsureBountyBody(uid, mindId, mind, state, replaceExisting: true);
                    break;
            }

            UpdateExtraObjectiveOffer(uid, component, mindId, mind, state);
        }
    }

    protected override void AppendAdminStatus(EntityUid uid,
        TraitorUltraRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var ultras = 0;
        var living = 0;

        foreach (var (mindId, _) in component.Minds)
        {
            if (!_roles.MindHasRole<TraitorUltraRoleComponent>(mindId))
                continue;

            ultras++;

            if (TryComp<MindComponent>(mindId, out var mind) &&
                mind.OwnedEntity is { } body &&
                _mobState.IsAlive(body))
            {
                living++;
            }
        }

        args.AddSection(
            Loc.GetString("game-rule-admin-status-traitor-ultra-title"),
            Loc.GetString("game-rule-admin-status-traitor-ultra-summary",
                ("candidates", component.Minds.Count),
                ("ultras", ultras),
                ("living", living)));
    }

    private void OnOpenContractAction(TraitorUltraOpenContractActionEvent args)
    {
        if (args.Handled ||
            !_mind.TryGetMind(args.Performer, out var mindId, out var mind))
        {
            return;
        }

        var query = EntityQueryEnumerator<TraitorUltraRuleComponent>();
        while (query.MoveNext(out var rule, out var component))
        {
            if (!component.Minds.TryGetValue(mindId, out var state) ||
                state.Stage != TraitorUltraStage.OfferOpen ||
                state.UpgradeOfferActionEntity != args.Action.Owner)
            {
                continue;
            }

            args.Handled = true;

            if (!TryGetEligibleAliveBody(mind, state, out var body) ||
                args.Performer != body)
            {
                InterruptUpgradeOffer(mindId, state);
                return;
            }

            if (Timing.CurTime >= state.NextEventTime)
            {
                DeclineUpgradeOffer(mindId, mind, state, "traitor-ultra-offer-expired-popup");
                return;
            }

            OpenUpgradeOfferEui(rule, mindId, mind);
            return;
        }
    }

    private void OnOpenExtraObjectiveOfferAction(TraitorUltraOpenExtraObjectiveOfferActionEvent args)
    {
        if (args.Handled ||
            !_mind.TryGetMind(args.Performer, out var mindId, out var mind))
        {
            return;
        }

        var query = EntityQueryEnumerator<TraitorUltraRuleComponent>();
        while (query.MoveNext(out var rule, out var component))
        {
            if (!component.Minds.TryGetValue(mindId, out var state) ||
                state.ExtraObjectiveOfferStatus != TraitorUltraExtraObjectiveOfferStatus.Open ||
                state.ExtraObjectiveOfferActionEntity != args.Action.Owner)
            {
                continue;
            }

            args.Handled = true;
            OpenExtraObjectiveOfferEui(rule, mindId, mind);
            return;
        }
    }

    private bool EnsureUpgradeOfferAction(
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state)
    {
        if (!TryGetEligibleAliveBody(mind, state, out var body))
            return false;

        return _actions.AddAction(body, ref state.UpgradeOfferActionEntity, component.UpgradeOfferAction, mindId);
    }

    private bool TryGetEligibleAliveBody(
        MindComponent mind,
        TraitorUltraMindState state,
        out EntityUid body)
    {
        body = default;
        if (state.EligibleBody is not { } eligible ||
            mind.OwnedEntity != eligible ||
            TerminatingOrDeleted(eligible) ||
            !TryComp<MobStateComponent>(eligible, out var mobState) ||
            mobState.CurrentState == MobState.Dead)
        {
            return false;
        }

        body = eligible;
        return true;
    }

    private void RemoveUpgradeOfferAction(TraitorUltraMindState state)
    {
        if (state.UpgradeOfferActionEntity is not { } action)
            return;

        _actions.RemoveAction(action);
        QueueDel(action);
        state.UpgradeOfferActionEntity = null;
    }

    private bool OpenUpgradeOfferEui(EntityUid rule, EntityUid mindId, MindComponent mind)
    {
        if (!TryGetSession(mind, out var session))
            return false;

        CloseUpgradeOfferEui(mindId);

        var eui = new TraitorUltraOfferEui(rule, mindId, this);
        _openUpgradeOfferEuis[mindId] = eui;
        _eui.OpenEui(eui, session);
        return true;
    }

    private void CloseUpgradeOfferEui(EntityUid mindId)
    {
        if (!_openUpgradeOfferEuis.Remove(mindId, out var eui))
            return;

        if (!eui.IsShutDown)
            eui.Close();
    }

    private void CloseAllUpgradeOfferEuis()
    {
        foreach (var mindId in _openUpgradeOfferEuis.Keys.ToArray())
        {
            CloseUpgradeOfferEui(mindId);
        }
    }

    public void OnUpgradeOfferEuiClosed(EntityUid mindId, TraitorUltraOfferEui eui)
    {
        if (_openUpgradeOfferEuis.TryGetValue(mindId, out var openEui) &&
            ReferenceEquals(openEui, eui))
        {
            _openUpgradeOfferEuis.Remove(mindId);
        }
    }

    private void DeclineUpgradeOffer(EntityUid mindId, MindComponent mind, TraitorUltraMindState state, string popup)
    {
        state.Stage = TraitorUltraStage.Declined;
        CloseUpgradeOfferEui(mindId);
        RemoveUpgradeOfferAction(state);
        ShowPopup(mind, popup, PopupType.Large);
    }

    public void HandleUpgradeOffer(EntityUid rule, EntityUid mindId, bool accepted)
    {
        if (!TryComp<TraitorUltraRuleComponent>(rule, out var component) ||
            !component.Minds.TryGetValue(mindId, out var state) ||
            !TryComp<MindComponent>(mindId, out var mind) ||
            state.Stage != TraitorUltraStage.OfferOpen)
        {
            return;
        }

        if (!TryGetEligibleAliveBody(mind, state, out _))
        {
            InterruptUpgradeOffer(mindId, state);
            return;
        }

        if (Timing.CurTime >= state.NextEventTime)
        {
            DeclineUpgradeOffer(mindId, mind, state, "traitor-ultra-offer-expired-popup");
            return;
        }

        if (!accepted)
        {
            DeclineUpgradeOffer(mindId, mind, state, "traitor-ultra-offer-declined-popup");
            return;
        }

        CloseUpgradeOfferEui(mindId);
        RemoveUpgradeOfferAction(state);
        if (!UpgradeTraitor(rule, component, mindId, mind, state))
        {
            state.Stage = TraitorUltraStage.Initial;
            state.NextEventTime = TimeSpan.Zero;
        }
    }

    public TraitorUltraOfferEuiState GetOfferState(EntityUid rule, EntityUid mindId)
    {
        string? oldCorp = null;
        string? newCorp = null;

        if (TryComp<TraitorUltraRuleComponent>(rule, out var component) &&
            component.Minds.TryGetValue(mindId, out var state))
        {
            state.OriginalCorporation ??= GetOriginalCorporation(rule, mindId);
            state.NewCorporation = PickNewCorporation(component, state.OriginalCorporation, state.NewCorporation);
            oldCorp = state.OriginalCorporation;
            newCorp = state.NewCorporation;
        }

        return new TraitorUltraOfferEuiState(
            Loc.GetString("traitor-ultra-offer-title"),
            Loc.GetString(
                "traitor-ultra-offer-body",
                ("oldCorp", LocalizeCorporation(oldCorp)),
                ("newCorp", LocalizeCorporation(newCorp))),
            Loc.GetString("traitor-ultra-offer-gains"),
            Loc.GetString("traitor-ultra-offer-losses"),
            Loc.GetString("traitor-ultra-offer-accept"),
            Loc.GetString("traitor-ultra-offer-decline"));
    }

    private bool EnsureExtraObjectiveOfferAction(
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state)
    {
        if (mind.OwnedEntity is not { } body || !Exists(body))
            return false;

        return _actions.AddAction(body, ref state.ExtraObjectiveOfferActionEntity, component.ExtraObjectiveOfferAction, mindId);
    }

    private void RemoveExtraObjectiveOfferAction(TraitorUltraMindState state)
    {
        if (state.ExtraObjectiveOfferActionEntity is not { } action)
            return;

        _actions.RemoveAction(action);
        QueueDel(action);
        state.ExtraObjectiveOfferActionEntity = null;
    }

    private bool OpenExtraObjectiveOfferEui(EntityUid rule, EntityUid mindId, MindComponent mind)
    {
        if (!TryGetSession(mind, out var session))
            return false;

        CloseExtraObjectiveOfferEui(mindId);

        var eui = new TraitorUltraExtraObjectiveOfferEui(rule, mindId, this);
        _openExtraObjectiveOfferEuis[mindId] = eui;
        _eui.OpenEui(eui, session);
        return true;
    }

    private void CloseExtraObjectiveOfferEui(EntityUid mindId)
    {
        if (!_openExtraObjectiveOfferEuis.Remove(mindId, out var eui))
            return;

        if (!eui.IsShutDown)
            eui.Close();
    }

    private void CloseAllExtraObjectiveOfferEuis()
    {
        foreach (var mindId in _openExtraObjectiveOfferEuis.Keys.ToArray())
        {
            CloseExtraObjectiveOfferEui(mindId);
        }
    }

    public void OnExtraObjectiveOfferEuiClosed(EntityUid mindId, TraitorUltraExtraObjectiveOfferEui eui)
    {
        if (_openExtraObjectiveOfferEuis.TryGetValue(mindId, out var openEui) &&
            ReferenceEquals(openEui, eui))
        {
            _openExtraObjectiveOfferEuis.Remove(mindId);
        }
    }

    public TraitorUltraExtraObjectiveOfferEuiState GetExtraObjectiveOfferState(EntityUid rule, EntityUid mindId)
    {
        var objectiveName = Loc.GetString("traitor-ultra-extra-objective-offer-objective-pending");
        var reward = FixedPoint2.Zero;
        string? newCorp = null;
        var body = "traitor-ultra-extra-objective-offer-body";

        if (TryComp<TraitorUltraRuleComponent>(rule, out var component) &&
            component.Minds.TryGetValue(mindId, out var state))
        {
            reward = component.ExtraObjectiveTelecrystals;
            newCorp = state.NewCorporation;
            body = OpensExtraObjectiveOfferImmediately(state.FirstPostUpgradeObjectivePrototype, component)
                ? "traitor-ultra-extra-objective-offer-body-immediate"
                : "traitor-ultra-extra-objective-offer-body";

            if (state.ExtraObjectiveOfferStatus == TraitorUltraExtraObjectiveOfferStatus.Open &&
                TryComp<MindComponent>(mindId, out var mind))
            {
                TryEnsurePendingExtraObjective((mindId, mind), component, state);
            }

            if (state.PendingExtraObjective is { } objective &&
                !TerminatingOrDeleted(objective))
            {
                objectiveName = Name(objective);
            }
        }

        return new TraitorUltraExtraObjectiveOfferEuiState(
            Loc.GetString("traitor-ultra-extra-objective-offer-title"),
            Loc.GetString(
                body,
                ("corp", LocalizeCorporation(newCorp))),
            Loc.GetString("traitor-ultra-extra-objective-offer-objective", ("objective", objectiveName)),
            Loc.GetString("traitor-ultra-extra-objective-offer-reward", ("amount", reward)),
            Loc.GetString("traitor-ultra-extra-objective-offer-accept"),
            Loc.GetString("traitor-ultra-extra-objective-offer-decline"));
    }

    public void HandleExtraObjectiveOffer(EntityUid rule, EntityUid mindId, bool accepted)
    {
        if (!TryComp<TraitorUltraRuleComponent>(rule, out var component) ||
            !component.Minds.TryGetValue(mindId, out var state) ||
            !TryComp<MindComponent>(mindId, out var mind) ||
            state.ExtraObjectiveOfferStatus != TraitorUltraExtraObjectiveOfferStatus.Open)
        {
            return;
        }

        if (!accepted)
        {
            DeclineExtraObjectiveOffer(mindId, mind, state);
            return;
        }

        if (!TryEnsurePendingExtraObjective((mindId, mind), component, state))
        {
            ShowPopup(mind, "traitor-ultra-extra-objective-offer-failed-popup", PopupType.Large);
            return;
        }

        var objective = state.PendingExtraObjective!.Value;
        if (!string.IsNullOrWhiteSpace(state.NewCorporation))
            _sharedObjectives.SetIssuer(objective, state.NewCorporation);

        CloseExtraObjectiveOfferEui(mindId);
        RemoveExtraObjectiveOfferAction(state);

        var added = false;
        if (!mind.Objectives.Contains(objective))
        {
            _mind.AddObjective(mindId, mind, objective);
            if (!string.IsNullOrWhiteSpace(state.NewCorporation))
                _sharedObjectives.SetIssuer(objective, state.NewCorporation);

            added = true;
        }

        state.PendingExtraObjective = null;
        state.PendingExtraObjectivePrototype = null;
        state.ExtraObjectiveOfferStatus = TraitorUltraExtraObjectiveOfferStatus.Accepted;

        if (added)
            AddTelecrystals(mindId, mind, component.ExtraObjectiveTelecrystals, component);

        ShowPopup(mind, "traitor-ultra-extra-objective-offer-accepted-popup", PopupType.Large);
    }

    private void DeclineExtraObjectiveOffer(EntityUid mindId, MindComponent mind, TraitorUltraMindState state)
    {
        state.ExtraObjectiveOfferStatus = TraitorUltraExtraObjectiveOfferStatus.Declined;
        CloseExtraObjectiveOfferEui(mindId);
        RemoveExtraObjectiveOfferAction(state);
        DeletePendingExtraObjective(state);
        ShowPopup(mind, "traitor-ultra-extra-objective-offer-declined-popup", PopupType.Large);
    }

    public void HandleRecruitOffer(EntityUid rule, EntityUid mindId, bool accepted)
    {
        if (!TryComp<TraitorUltraRuleComponent>(rule, out var component) ||
            !TryComp<MindComponent>(mindId, out var mind))
        {
            return;
        }

        if (!component.PendingRecruitOffers.Remove(mindId, out var corporation))
            return;

        if (!accepted || _roles.MindIsAntagonist(mindId))
            return;

        _roles.MindAddRole(mindId, component.RecruitMindRole, mind, silent: true);

        TraitorRuleComponent? traitorRule = null;
        if (TryComp<TraitorRuleComponent>(rule, out var existingTraitorRule))
        {
            traitorRule = existingTraitorRule;
            traitorRule.TraitorMinds.Add(mindId);

            if (!string.IsNullOrWhiteSpace(corporation))
                traitorRule.ObjectiveIssuersByMind[mindId] = corporation;
        }

        var assignedObjectives = new List<EntityUid>();
        if (!TryAssignRecruitObjectives((mindId, mind), component, corporation, assignedObjectives))
        {
            foreach (var objective in assignedObjectives)
                RemoveObjective((mindId, mind), objective);

            _roles.MindRemoveRole<TraitorRoleComponent>((mindId, mind));
            traitorRule?.TraitorMinds.Remove(mindId);
            traitorRule?.ObjectiveIssuersByMind.Remove(mindId);
            SendMindMessage(mind, "traitor-ultra-recruit-failed-no-objective", Color.OrangeRed);
            return;
        }

        if (mind.OwnedEntity is { } owned)
            _uplink.AddUplink(owned, component.RecruitTelecrystals, giveDiscounts: true);

        if (TryGetSession(mind, out var session))
        {
            _antag.SendBriefing(
                session,
                Loc.GetString("traitor-ultra-recruit-briefing", ("corp", LocalizeCorporation(corporation))),
                Color.Yellow,
                null);
        }
    }

    private bool TryAssignRecruitObjectives(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        string? issuer,
        List<EntityUid> assignedObjectives)
    {
        var excludedStealObjectives = _traitorRule.GetAssignedStealObjectivePrototypes(mind.Owner);
        var difficulty = 0f;
        for (var pick = 0;
             pick < component.RecruitObjectiveMaxPicks && component.RecruitObjectiveMaxDifficulty > difficulty;
             pick++)
        {
            var remainingDifficulty = component.RecruitObjectiveMaxDifficulty - difficulty;
            if (_objectives.GetRandomObjective(mind.Owner, mind.Comp, component.RecruitObjectiveGroups, remainingDifficulty, excludedStealObjectives) is not { } objective)
                continue;

            if (!string.IsNullOrWhiteSpace(issuer))
                _sharedObjectives.SetIssuer(objective, issuer);

            _mind.AddObjective(mind.Owner, mind.Comp, objective);

            if (!string.IsNullOrWhiteSpace(issuer))
                _sharedObjectives.SetIssuer(objective, issuer);

            assignedObjectives.Add(objective);
            TrackAssignedStealObjective(excludedStealObjectives, objective);
            difficulty += Comp<ObjectiveComponent>(objective).Difficulty;
        }

        return assignedObjectives.Count > 0;
    }

    private bool TryAssignBaseObjectives(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        string? issuer)
    {
        var excludedStealObjectives = _traitorRule.GetAssignedStealObjectivePrototypes(mind.Owner);
        var assigned = false;
        var difficulty = 0f;
        for (var pick = 0;
             pick < component.BaseObjectiveMaxPicks && component.BaseObjectiveMaxDifficulty > difficulty;
             pick++)
        {
            var remainingDifficulty = component.BaseObjectiveMaxDifficulty - difficulty;
            if (_objectives.GetRandomObjective(mind.Owner, mind.Comp, component.BaseObjectiveGroups, remainingDifficulty, excludedStealObjectives) is not { } objective)
                continue;

            if (!string.IsNullOrWhiteSpace(issuer))
                _sharedObjectives.SetIssuer(objective, issuer);

            _mind.AddObjective(mind.Owner, mind.Comp, objective);

            if (!string.IsNullOrWhiteSpace(issuer))
                _sharedObjectives.SetIssuer(objective, issuer);

            TrackAssignedStealObjective(excludedStealObjectives, objective);
            difficulty += Comp<ObjectiveComponent>(objective).Difficulty;
            assigned = true;
        }

        return assigned;
    }

    private bool AssignInitialObjectives(Entity<MindComponent> mind, TraitorUltraRuleComponent component, TraitorUltraMindState state)
    {
        if (HasHighRiskStealObjective(mind, component))
        {
            return TryAssignCommandKill(mind, component, state) ||
                   TryAssignHighRiskStealPackage(mind, component, state);
        }

        var preferStealPackage = _random.Prob(0.5f);
        return preferStealPackage
            ? TryAssignHighRiskStealPackage(mind, component, state) || TryAssignCommandKill(mind, component, state)
            : TryAssignCommandKill(mind, component, state) || TryAssignHighRiskStealPackage(mind, component, state);
    }

    private bool HasHighRiskStealObjective(Entity<MindComponent> mind, TraitorUltraRuleComponent component)
    {
        foreach (var objective in mind.Comp.Objectives)
        {
            if (TerminatingOrDeleted(objective))
                continue;

            var prototype = MetaData(objective).EntityPrototype?.ID;
            if (prototype != null && component.HighRiskStealObjectives.Contains(prototype))
                return true;
        }

        return false;
    }

    private bool TryAssignHighRiskStealPackage(Entity<MindComponent> mind, TraitorUltraRuleComponent component, TraitorUltraMindState state)
    {
        var excludedStealObjectives = _traitorRule.GetAssignedStealObjectivePrototypes(mind.Owner);
        var available = component.HighRiskStealObjectives
            .Where(proto => !excludedStealObjectives.Contains(proto))
            .ToList();
        var assigned = new List<EntityUid>();

        while (available.Count > 0 && assigned.Count < 2)
        {
            var proto = _random.PickAndTake(available);
            if (!TryCreateAndAddObjective(mind, proto, state.OriginalCorporation, out var objective))
                continue;

            assigned.Add(objective);
            excludedStealObjectives.Add(proto);
        }

        if (assigned.Count == 2)
        {
            state.InitialObjectives.AddRange(assigned);
            return true;
        }

        foreach (var objective in assigned)
            RemoveObjective(mind, objective);

        return false;
    }

    private void TrackAssignedStealObjective(HashSet<string> excludedStealObjectives, EntityUid objective)
    {
        if (HasComp<Content.Server.Objectives.Components.StealConditionComponent>(objective) &&
            MetaData(objective).EntityPrototype?.ID is { } prototype)
        {
            excludedStealObjectives.Add(prototype);
        }
    }

    private bool TryAssignCommandKill(Entity<MindComponent> mind, TraitorUltraRuleComponent component, TraitorUltraMindState state)
    {
        if (!TryCreateAndAddObjective(mind, component.CommandKillObjective, state.OriginalCorporation, out var objective))
            return false;

        state.InitialObjectives.Add(objective);
        return true;
    }

    private bool TryPickObjective(Entity<MindComponent> mind, IReadOnlyList<EntProtoId> prototypes, string? issuer, out EntityUid objective)
    {
        var available = prototypes.ToList();
        while (available.Count > 0)
        {
            var proto = _random.PickAndTake(available);
            if (TryCreateAndAddObjective(mind, proto, issuer, out objective))
                return true;
        }

        objective = default;
        return false;
    }

    private bool TryCreateAndAddObjective(Entity<MindComponent> mind, EntProtoId prototype, string? issuer, out EntityUid objective)
    {
        objective = default;
        if (!_proto.HasIndex<EntityPrototype>(prototype))
        {
            Log.Warning($"TraitorUltra objective prototype {prototype} does not exist.");
            return false;
        }

        var created = _sharedObjectives.TryCreateObjective(mind.Owner, mind.Comp, prototype);
        if (created == null)
            return false;

        objective = created.Value;
        if (!string.IsNullOrWhiteSpace(issuer))
            _sharedObjectives.SetIssuer(objective, issuer);

        _mind.AddObjective(mind.Owner, mind.Comp, objective);

        if (!string.IsNullOrWhiteSpace(issuer))
            _sharedObjectives.SetIssuer(objective, issuer);

        return true;
    }

    private void RemoveObjective(Entity<MindComponent> mind, EntityUid objective)
    {
        var index = mind.Comp.Objectives.IndexOf(objective);
        if (index >= 0)
            _mind.TryRemoveObjective(mind.Owner, mind.Comp, index);
        else
            Del(objective);
    }

    private bool InitialObjectivesCompleted(EntityUid mindId, MindComponent mind, TraitorUltraRuleComponent component)
    {
        var requiredObjectives = new List<EntityUid>();

        foreach (var objective in mind.Objectives.ToArray())
        {
            if (TerminatingOrDeleted(objective))
                return false;

            if (ObjectiveIgnoredForUpgradeCompletion(objective, component))
            {
                RemoveObjective((mindId, mind), objective);
                continue;
            }

            if (ObjectiveOptionalForUpgradeCompletion(objective, component))
                continue;

            requiredObjectives.Add(objective);
            if (!_objectives.IsCompleted(objective, (mindId, mind)))
                return false;
        }

        if (requiredObjectives.Count == 0)
            return false;

        foreach (var objective in requiredObjectives)
            _sharedObjectives.LockCompletion(objective);

        return true;
    }

    private bool ObjectiveIgnoredForUpgradeCompletion(EntityUid objective, TraitorUltraRuleComponent component)
    {
        var prototype = MetaData(objective).EntityPrototype?.ID;
        if (prototype == null)
            return false;

        foreach (var ignored in component.UpgradeCompletionIgnoredObjectives)
        {
            if (ignored == prototype)
                return true;
        }

        return false;
    }

    private bool ObjectiveOptionalForUpgradeCompletion(EntityUid objective, TraitorUltraRuleComponent component)
    {
        var prototype = MetaData(objective).EntityPrototype?.ID;
        if (prototype == null)
            return false;

        foreach (var optional in component.UpgradeCompletionOptionalObjectives)
        {
            if (optional == prototype)
                return true;
        }

        return false;
    }

    private bool UpgradeTraitor(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state,
        bool announceBounty = true,
        bool requireCompletedObjectives = true)
    {
        if (!TryGetEligibleAliveBody(mind, state, out _))
            return false;

        var objectivesCompleted = InitialObjectivesCompleted(mindId, mind, component);
        if (requireCompletedObjectives && !objectivesCompleted)
            return false;

        if (!EnsureUpgradeResources((mindId, mind), component, state))
            return false;

        state.OriginalCorporation ??= GetOriginalCorporation(rule, mindId);
        state.NewCorporation = PickNewCorporation(component, state.OriginalCorporation, state.NewCorporation);
        state.AgentName = GetMindCharacterName(mind);
        state.BountyReward = component.TraitorKillRewardTelecrystals;
        state.Stage = TraitorUltraStage.Upgraded;
        state.BountyAnnouncementSuppressed = !announceBounty;
        state.NextEventTime = announceBounty
            ? Timing.CurTime + component.BountyPreparationTime
            : TimeSpan.Zero;

        UpgradeTraitorRole(mindId, mind, component);

        RemoveTraitorMindFromAllRules(mindId);

        if (TryAssignPostUpgradeObjective((mindId, mind), component, state, out var firstPostUpgradeObjective))
            InitializeExtraObjectiveOfferState(rule, component, mindId, mind, state, firstPostUpgradeObjective);
        else
            Log.Error($"Failed to assign a post-upgrade TraitorUltra objective to {ToPrettyString(mindId)}.");

        if (!TryAssignPostUpgradeSurviveObjective((mindId, mind), component, state.NewCorporation))
            Log.Error($"Failed to assign a post-upgrade TraitorUltra survive objective to {ToPrettyString(mindId)}.");

        AddTelecrystals(mindId, mind, component.UpgradeTelecrystals, component);
        SendUpgradeBriefing(mind, component, state);
        AppendUpgradeBriefing(mindId, state);
        LogTraitorUltraUpgrade(mindId, mind, state);

        if (announceBounty)
            QueueDelayedAction(component.BountyPreparationTime, rule, mindId, TraitorUltraDelayedActionType.AnnounceBounty);

        return true;
    }

    private void RemoveTraitorMindFromAllRules(EntityUid mindId)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out _, out var traitorRule))
        {
            traitorRule.TraitorMinds.Remove(mindId);
            traitorRule.ObjectiveIssuersByMind.Remove(mindId);
        }
    }

    private void LogTraitorUltraUpgrade(EntityUid mindId, MindComponent mind, TraitorUltraMindState state)
    {
        var characterName = state.AgentName ?? GetMindCharacterName(mind) ?? Loc.GetString("generic-unknown-title");
        var playerName = TryGetSession(mind, out var session) ? session.Name : Loc.GetString("generic-unknown-title");
        var oldCorporation = LocalizeCorporation(state.OriginalCorporation);
        var newCorporation = LocalizeCorporation(state.NewCorporation);
        var message = $"Агент {characterName} ({playerName}) стал ультра-предателем: {oldCorporation} -> {newCorporation}.";

        _chatManager.SendAdminAlert(message);
        _adminLogger.Add(LogType.AntagSelection, LogImpact.High, $"{ToPrettyString(mindId)} became TraitorUltra. Player: {playerName}, character: {characterName}, corporation: {oldCorporation} -> {newCorporation}.");
    }

    private void InitializeExtraObjectiveOfferState(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state,
        EntityUid firstPostUpgradeObjective)
    {
        state.FirstPostUpgradeObjective = firstPostUpgradeObjective;
        state.FirstPostUpgradeObjectivePrototype = MetaData(firstPostUpgradeObjective).EntityPrototype?.ID;

        if (OpensExtraObjectiveOfferImmediately(state.FirstPostUpgradeObjectivePrototype, component))
            TryOpenExtraObjectiveOffer(rule, component, mindId, mind, state);
        else
            state.ExtraObjectiveOfferStatus = TraitorUltraExtraObjectiveOfferStatus.WaitingForPrimaryCompletion;
    }

    private void UpdateExtraObjectiveOffer(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state)
    {
        switch (state.ExtraObjectiveOfferStatus)
        {
            case TraitorUltraExtraObjectiveOfferStatus.WaitingForPrimaryCompletion:
                if (IsFirstPostUpgradeObjectiveCompleted((mindId, mind), state))
                    TryOpenExtraObjectiveOffer(rule, component, mindId, mind, state);
                return;

            case TraitorUltraExtraObjectiveOfferStatus.Open:
                EnsureExtraObjectiveOfferAction(component, mindId, mind, state);
                TryEnsurePendingExtraObjective((mindId, mind), component, state);
                return;

            default:
                return;
        }
    }

    private bool TryOpenExtraObjectiveOffer(
        EntityUid rule,
        TraitorUltraRuleComponent component,
        EntityUid mindId,
        MindComponent mind,
        TraitorUltraMindState state)
    {
        if (state.ExtraObjectiveOfferStatus is
            TraitorUltraExtraObjectiveOfferStatus.Accepted or
            TraitorUltraExtraObjectiveOfferStatus.Declined ||
            state.FirstPostUpgradeObjective == null)
        {
            return false;
        }

        if (state.ExtraObjectiveOfferStatus == TraitorUltraExtraObjectiveOfferStatus.Open)
            return true;

        TryEnsurePendingExtraObjective((mindId, mind), component, state);

        state.ExtraObjectiveOfferStatus = TraitorUltraExtraObjectiveOfferStatus.Open;
        EnsureExtraObjectiveOfferAction(component, mindId, mind, state);
        ShowPopup(mind, "traitor-ultra-extra-objective-offer-ready-popup", PopupType.Large);
        OpenExtraObjectiveOfferEui(rule, mindId, mind);
        return true;
    }

    private bool OpensExtraObjectiveOfferImmediately(string? prototype, TraitorUltraRuleComponent component)
    {
        if (prototype == null)
            return false;

        foreach (var eligible in component.ExtraObjectiveEligibleFirstObjectives)
        {
            if (eligible == prototype)
                return true;
        }

        return false;
    }

    private bool IsFirstPostUpgradeObjectiveCompleted(Entity<MindComponent> mind, TraitorUltraMindState state)
    {
        return state.FirstPostUpgradeObjective is { } objective &&
               !TerminatingOrDeleted(objective) &&
               _objectives.IsCompleted(objective, mind);
    }

    private bool TryEnsurePendingExtraObjective(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state)
    {
        if (state.PendingExtraObjective is { } existing &&
            !TerminatingOrDeleted(existing))
        {
            return true;
        }

        state.PendingExtraObjective = null;
        state.PendingExtraObjectivePrototype = null;

        if (!TryPickExtraObjective(mind, component, state, out var objective))
            return false;

        state.PendingExtraObjective = objective;
        state.PendingExtraObjectivePrototype = MetaData(objective).EntityPrototype?.ID;
        return true;
    }

    private bool TryPickExtraObjective(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state,
        out EntityUid objective)
    {
        var excluded = GetAssignedObjectivePrototypes(mind.Comp);

        if (!string.IsNullOrWhiteSpace(state.FirstPostUpgradeObjectivePrototype))
            excluded.Add(state.FirstPostUpgradeObjectivePrototype);

        if (_random.Prob(component.RarePostUpgradeObjectiveProbability) &&
            TryPickObjectiveForOffer(mind, component.RarePostUpgradeObjectives, state.NewCorporation, excluded, out objective))
        {
            return true;
        }

        if (TryPickObjectiveForOffer(mind, component.PostUpgradeObjectives, state.NewCorporation, excluded, out objective))
            return true;

        return TryPickObjectiveForOffer(mind, component.RarePostUpgradeObjectives, state.NewCorporation, excluded, out objective);
    }

    private HashSet<string> GetAssignedObjectivePrototypes(MindComponent mind)
    {
        var prototypes = new HashSet<string>();
        foreach (var objective in mind.Objectives)
        {
            if (!TerminatingOrDeleted(objective) &&
                MetaData(objective).EntityPrototype?.ID is { } prototype)
            {
                prototypes.Add(prototype);
            }
        }

        return prototypes;
    }

    private bool TryPickObjectiveForOffer(
        Entity<MindComponent> mind,
        IReadOnlyList<EntProtoId> prototypes,
        string? issuer,
        HashSet<string> excluded,
        out EntityUid objective)
    {
        var available = prototypes.ToList();
        while (available.Count > 0)
        {
            var proto = _random.PickAndTake(available);
            if (excluded.Contains(proto.Id))
                continue;

            if (!TryCreateObjectiveForOffer(mind, proto, issuer, out objective))
                continue;

            excluded.Add(proto.Id);
            return true;
        }

        objective = default;
        return false;
    }

    private bool TryCreateObjectiveForOffer(Entity<MindComponent> mind, EntProtoId prototype, string? issuer, out EntityUid objective)
    {
        objective = default;
        if (!_proto.HasIndex<EntityPrototype>(prototype))
        {
            Log.Warning($"TraitorUltra objective prototype {prototype} does not exist.");
            return false;
        }

        var created = _sharedObjectives.TryCreateObjective(mind.Owner, mind.Comp, prototype);
        if (created == null)
            return false;

        objective = created.Value;
        if (!string.IsNullOrWhiteSpace(issuer))
            _sharedObjectives.SetIssuer(objective, issuer);

        return true;
    }

    private void DeletePendingExtraObjective(TraitorUltraMindState state)
    {
        if (state.PendingExtraObjective is { } objective &&
            !TerminatingOrDeleted(objective))
        {
            Del(objective);
        }

        state.PendingExtraObjective = null;
        state.PendingExtraObjectivePrototype = null;
    }

    private bool TryAssignPostUpgradeObjective(
        Entity<MindComponent> mind,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state,
        out EntityUid objective)
    {
        if (_random.Prob(component.RarePostUpgradeObjectiveProbability))
        {
            if (TryPickObjective(mind, component.RarePostUpgradeObjectives, state.NewCorporation, out objective))
                return true;
        }

        if (TryPickObjective(mind, component.PostUpgradeObjectives, state.NewCorporation, out objective))
            return true;

        return TryPickObjective(mind, component.RarePostUpgradeObjectives, state.NewCorporation, out objective);
    }

    private bool TryAssignPostUpgradeSurviveObjective(Entity<MindComponent> mind, TraitorUltraRuleComponent component, string? issuer)
    {
        return TryCreateAndAddObjective(mind, component.PostUpgradeSurviveObjective, issuer, out _);
    }

    private void UpgradeTraitorRole(EntityUid mindId, MindComponent mind, TraitorUltraRuleComponent component)
    {
        var updated = false;
        foreach (var roleUid in mind.MindRoleContainer.ContainedEntities)
        {
            if (!HasComp<TraitorRoleComponent>(roleUid) ||
                !TryComp<MindRoleComponent>(roleUid, out var role))
            {
                continue;
            }

            role.AntagPrototype = "TraitorUltra";
            role.Subtype = "role-subtype-traitor-ultra";
            EnsureComp<TraitorUltraRoleComponent>(roleUid);
            Dirty(roleUid, role);
            updated = true;
        }

        if (!updated)
        {
            Log.Warning($"TraitorUltra upgrade could not find a TraitorRole on {ToPrettyString(mindId)}; adding Ultra role silently.");
            _roles.MindAddRole(mindId, component.UltraMindRole, mind, silent: true);
        }

        _roles.RefreshMindRoleType((mindId, mind));
    }

    private void AppendUpgradeBriefing(EntityUid mindId, TraitorUltraMindState state)
    {
        if (!_roles.MindHasRole<TraitorRoleComponent>(mindId, out var traitorRole))
            return;

        var roleUid = traitorRole.Value.Owner;
        var briefing = EnsureComp<RoleBriefingComponent>(roleUid);
        var ultraBriefing = Loc.GetString(
            "traitor-ultra-role-briefing-memory",
            ("oldCorp", LocalizeCorporation(state.OriginalCorporation)),
            ("newCorp", LocalizeCorporation(state.NewCorporation)));

        briefing.Briefing = string.IsNullOrWhiteSpace(briefing.Briefing)
            ? ultraBriefing
            : $"{briefing.Briefing}\n{ultraBriefing}";
        Dirty(roleUid, briefing);
    }

    private void SendUpgradeBriefing(MindComponent mind, TraitorUltraRuleComponent component, TraitorUltraMindState state)
    {
        if (!TryGetSession(mind, out var session))
            return;

        var briefing = Loc.GetString(
            "traitor-ultra-upgrade-briefing",
            ("oldCorp", LocalizeCorporation(state.OriginalCorporation)),
            ("newCorp", LocalizeCorporation(state.NewCorporation)));

        _antag.SendBriefing(session, briefing, Color.Yellow, component.UpgradeSound);
    }

    private void AnnounceBounty(EntityUid rule, TraitorUltraRuleComponent component, EntityUid mindId, MindComponent? mind, TraitorUltraMindState state)
    {
        if (state.BountyAnnounced)
            return;

        state.BountyAnnounced = true;
        state.Stage = TraitorUltraStage.BountyAnnounced;

        var name = state.AgentName ?? (mind == null ? null : GetMindCharacterName(mind));
        state.AgentName = name;

        var announcement = Loc.GetString(
            GetBountyAnnouncementLocId(state.OriginalCorporation),
            ("oldCorp", LocalizeCorporation(state.OriginalCorporation)),
            ("newCorp", LocalizeCorporation(state.NewCorporation)),
            ("agent", name ?? Loc.GetString("generic-unknown-title")),
            ("reward", state.BountyReward));

        _chat.DispatchGlobalAnnouncement(
            announcement,
            sender: LocalizeCorporation(state.OriginalCorporation),
            playSound: true,
            announcementSound: component.BountyAnnouncementSound,
            colorOverride: Color.OrangeRed,
            originalMessage: announcement,
            voice: PickRandomAnnouncementVoice());

        AssignBountyKillObjectives(component, mindId, state);

        if (mind != null)
            EnsureBountyBody(rule, mindId, mind, state, replaceExisting: true);
    }

    private void AssignBountyKillObjectives(
        TraitorUltraRuleComponent component,
        EntityUid targetMindId,
        TraitorUltraMindState state)
    {
        if (!TryComp<MindComponent>(targetMindId, out var targetMind))
            return;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mindId == targetMindId ||
                !IsTraitorOrUltraMind(mindId) ||
                HasBountyKillObjectiveForTarget((mindId, mind), component.BountyKillObjective, targetMindId))
            {
                continue;
            }

            if (!TryCreateBountyKillObjective((mindId, mind), targetMindId, targetMind, component, state.OriginalCorporation))
                Log.Warning($"Failed to assign TraitorUltra bounty kill objective for {ToPrettyString(targetMindId)} to {ToPrettyString(mindId)}.");
        }
    }

    private bool IsTraitorOrUltraMind(EntityUid mindId)
    {
        return _roles.MindHasRole<TraitorRoleComponent>(mindId) ||
               _roles.MindHasRole<TraitorUltraRoleComponent>(mindId);
    }

    private bool HasBountyKillObjectiveForTarget(Entity<MindComponent> mind, EntProtoId prototype, EntityUid targetMindId)
    {
        foreach (var objective in mind.Comp.Objectives)
        {
            if (TerminatingOrDeleted(objective) ||
                MetaData(objective).EntityPrototype?.ID != prototype.Id ||
                !TryComp<TargetObjectiveComponent>(objective, out var targetObjective))
            {
                continue;
            }

            if (targetObjective.Target == targetMindId)
                return true;
        }

        return false;
    }

    private bool TryCreateBountyKillObjective(
        Entity<MindComponent> mind,
        EntityUid targetMindId,
        MindComponent targetMind,
        TraitorUltraRuleComponent component,
        string? issuer)
    {
        if (!_proto.HasIndex<EntityPrototype>(component.BountyKillObjective))
        {
            Log.Warning($"TraitorUltra bounty objective prototype {component.BountyKillObjective} does not exist.");
            return false;
        }

        var created = _sharedObjectives.TryCreateObjective(mind.Owner, mind.Comp, component.BountyKillObjective, force: true);
        if (created == null)
            return false;

        var objective = created.Value;
        if (!TryComp<TargetObjectiveComponent>(objective, out var targetObjective))
        {
            Del(objective);
            return false;
        }

        _targetObjectives.SetTarget(objective, targetMindId, targetObjective);
        SetTargetObjectiveTitle(objective, targetMindId, targetMind, targetObjective);

        if (!string.IsNullOrWhiteSpace(issuer))
            _sharedObjectives.SetIssuer(objective, issuer);

        _mind.AddObjective(mind.Owner, mind.Comp, objective);

        if (!string.IsNullOrWhiteSpace(issuer))
            _sharedObjectives.SetIssuer(objective, issuer);

        return true;
    }

    private void SetTargetObjectiveTitle(
        EntityUid objective,
        EntityUid targetMindId,
        MindComponent targetMind,
        TargetObjectiveComponent targetObjective)
    {
        var targetName = targetMind.CharacterName;
        if (string.IsNullOrWhiteSpace(targetName) && targetMind.OwnedEntity is { } owned)
            targetName = Name(owned);

        _metaData.SetEntityName(
            objective,
            Loc.GetString(
                targetObjective.Title,
                ("targetName", string.IsNullOrWhiteSpace(targetName) ? "Unknown" : targetName),
                ("job", _jobs.MindTryGetJobName(targetMindId))),
            MetaData(objective));
    }

    private void EnsureBountyBody(EntityUid rule, EntityUid mindId, MindComponent mind, TraitorUltraMindState state, bool replaceExisting)
    {
        if (state.BountyResolved)
            return;

        if (!replaceExisting && state.BountyBody is { } existingBody && !TerminatingOrDeleted(existingBody))
        {
            var existingComp = EnsureComp<TraitorUltraBountyTargetComponent>(existingBody);
            existingComp.Rule = rule;
            existingComp.MindId = mindId;
            return;
        }

        if (mind.OwnedEntity is not { } body || TerminatingOrDeleted(body))
            return;

        if (replaceExisting &&
            state.BountyBody is { } oldBody &&
            oldBody != body &&
            !TerminatingOrDeleted(oldBody))
        {
            RemCompDeferred<TraitorUltraBountyTargetComponent>(oldBody);
        }

        state.BountyBody = body;
        var comp = EnsureComp<TraitorUltraBountyTargetComponent>(body);
        comp.Rule = rule;
        comp.MindId = mindId;
    }

    private void OnBountyDamageChanged(Entity<TraitorUltraBountyTargetComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
            {
                ent.Comp.DamageByMind.Clear();
                ent.Comp.DamageSourceByMind.Clear();
            }

            return;
        }

        var delta = args.DamageDelta.GetTotal();
        if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
            {
                ent.Comp.DamageByMind.Clear();
                ent.Comp.DamageSourceByMind.Clear();
            }

            return;
        }

        if (delta <= FixedPoint2.Zero ||
            !TryGetDamageSourceMind(args.Origin, out var sourceMindId, out _, out var sourceEntity) ||
            sourceMindId == ent.Comp.MindId && sourceEntity == ent.Owner)
        {
            return;
        }

        ent.Comp.DamageByMind[sourceMindId] = ent.Comp.DamageByMind.GetValueOrDefault(sourceMindId) + delta;
        ent.Comp.DamageSourceByMind[sourceMindId] = sourceEntity;
    }

    private void OnBountyMobStateChanged(Entity<TraitorUltraBountyTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState >= args.NewMobState)
            return;

        ResolveBountyDeath(ent, args.Origin);
    }

    private void OnBountyGibbedBeforeDeletion(Entity<TraitorUltraBountyTargetComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        ResolveBountyDeletionAfterDeath(ent);
    }

    private void OnBountyTerminating(Entity<TraitorUltraBountyTargetComponent> ent, ref EntityTerminatingEvent args)
    {
        ResolveBountyDeletionAfterDeath(ent);
    }

    private void ResolveBountyDeletionAfterDeath(Entity<TraitorUltraBountyTargetComponent> ent)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobState) || mobState.CurrentState != MobState.Dead)
            return;

        ResolveBountyDeath(ent, null);
    }

    private void ResolveBountyDeath(Entity<TraitorUltraBountyTargetComponent> ent, EntityUid? origin)
    {
        if (!TryComp<TraitorUltraRuleComponent>(ent.Comp.Rule, out var ruleComp) ||
            !ruleComp.Minds.TryGetValue(ent.Comp.MindId, out var state) ||
            state.BountyResolved)
        {
            return;
        }

        state.BountyResolved = true;
        state.Stage = TraitorUltraStage.Resolved;
        if (!TerminatingOrDeleted(ent.Owner))
            RemCompDeferred<TraitorUltraBountyTargetComponent>(ent);

        if (!TryResolveKillerMind(ent.Comp, origin, out var killerMindId, out var killerMind, out var killerEntity) ||
            killerMindId == ent.Comp.MindId && (killerEntity == null || killerEntity == ent.Owner))
        {
            return;
        }

        if (_roles.MindHasRole<TraitorRoleComponent>(killerMindId))
        {
            SendMindMessage(killerMind, "traitor-ultra-bounty-traitor-kill-message", Color.Yellow);
            QueueDelayedAction(ruleComp.RewardDelay, ent.Comp.Rule, killerMindId, TraitorUltraDelayedActionType.GiveTraitorKillReward);
            return;
        }

        if (IsCaptainMind(killerMindId))
        {
            if (!TryCreditCaptainKillReward(killerMind, killerEntity, ruleComp))
                Log.Warning($"Failed to credit TraitorUltra captain kill reward to {ToPrettyString(killerMindId)}.");

            SendMindMessage(killerMind, "traitor-ultra-bounty-captain-kill-message", Color.LightSkyBlue);
            return;
        }

        if (HasRealMindShield(killerMind, killerEntity))
        {
            if (!TryCreditSecurityKillReward(ent.Owner, killerMind, killerEntity, ruleComp))
                Log.Warning($"Failed to credit TraitorUltra security kill reward for {ToPrettyString(killerMindId)}.");

            SendMindMessage(killerMind, "traitor-ultra-bounty-security-kill-message", Color.LightSkyBlue);
            return;
        }

        if (!_roles.MindIsAntagonist(killerMindId) && IsPlayerMind(killerMind))
        {
            SendMindMessage(killerMind, "traitor-ultra-bounty-crew-kill-message", Color.Yellow);
            QueueDelayedAction(ruleComp.RewardDelay, ent.Comp.Rule, killerMindId, TraitorUltraDelayedActionType.OpenRecruitOffer, state.OriginalCorporation);
        }
    }

    private bool TryResolveKillerMind(
        TraitorUltraBountyTargetComponent comp,
        EntityUid? origin,
        out EntityUid mindId,
        out MindComponent mind,
        out EntityUid? sourceEntity)
    {
        if (TryGetDamageSourceMind(origin, out mindId, out mind, out var directSource))
        {
            sourceEntity = directSource;
            return true;
        }

        var highest = FixedPoint2.Zero;
        mindId = default;
        mind = default!;
        sourceEntity = null;
        var found = false;

        foreach (var (candidateMindId, damage) in comp.DamageByMind)
        {
            if (damage <= highest || !TryComp<MindComponent>(candidateMindId, out var candidateMind))
                continue;

            mindId = candidateMindId;
            mind = candidateMind;
            sourceEntity = comp.DamageSourceByMind.GetValueOrDefault(candidateMindId);
            highest = damage;
            found = true;
        }

        return found;
    }

    private bool TryGetDamageSourceMind(EntityUid? source, out EntityUid mindId, out MindComponent mind, out EntityUid sourceEntity)
    {
        mindId = default;
        mind = default!;
        sourceEntity = default;

        if (source == null)
            return false;

        if (TryGetMind(source.Value, out mindId, out mind))
        {
            sourceEntity = source.Value;
            return true;
        }

        if (TryGetProjectileSourceMind(source.Value, out mindId, out mind, out sourceEntity))
            return true;

        var current = source.Value;
        for (var i = 0; i < SourceParentSearchDepth; i++)
        {
            if (!TryComp(current, out TransformComponent? transform))
                return false;

            var parent = transform.ParentUid;
            if (parent == current)
                return false;

            if (TryGetMind(parent, out mindId, out mind))
            {
                sourceEntity = parent;
                return true;
            }

            if (TryGetProjectileSourceMind(parent, out mindId, out mind, out sourceEntity))
                return true;

            current = parent;
        }

        return false;
    }

    private bool TryGetProjectileSourceMind(EntityUid uid, out EntityUid mindId, out MindComponent mind, out EntityUid sourceEntity)
    {
        mindId = default;
        mind = default!;
        sourceEntity = default;

        if (!TryComp<ProjectileComponent>(uid, out var projectile))
            return false;

        if (projectile.Shooter != null && TryGetMind(projectile.Shooter.Value, out mindId, out mind))
        {
            sourceEntity = projectile.Shooter.Value;
            return true;
        }

        if (projectile.Weapon == null || !TryGetMind(projectile.Weapon.Value, out mindId, out mind))
            return false;

        sourceEntity = projectile.Weapon.Value;
        return true;
    }

    private bool TryGetMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
            return false;

        mindId = mindContainer.Mind.Value;
        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return false;

        mind = mindComp;
        return true;
    }

    private bool IsCaptainMind(EntityUid mindId)
    {
        return _jobs.MindTryGetJobId(mindId, out var jobId) &&
               jobId != null &&
               jobId.Value == "Captain";
    }

    private bool HasRealMindShield(MindComponent killerMind, EntityUid? entity)
    {
        var killerEntity = GetKillerRewardEntity(killerMind, entity);
        return killerEntity != null && HasComp<MindShieldComponent>(killerEntity.Value);
    }

    private bool TryCreditCaptainKillReward(
        MindComponent killerMind,
        EntityUid? sourceEntity,
        TraitorUltraRuleComponent component)
    {
        if (component.CaptainKillRewardCredits <= 0)
            return true;

        var killerEntity = GetKillerRewardEntity(killerMind, sourceEntity);
        if (killerEntity == null || !_idCard.TryFindIdCard(killerEntity.Value, out var idCard))
            return false;

        if (!_bankManager.TryGetBankAccount(idCard.Owner, out var account))
        {
            account = _bankManager.CreateNewBankAccount(idCard.Owner);
            if (account == null)
                return false;

            account.Value.Comp.AccountName = GetRewardAccountName(killerEntity.Value, idCard);
            idCard.Comp.StoredBankAccountNumber = account.Value.Comp.AccountNumber;
            Dirty(idCard);
            Dirty(account.Value);
        }
        else if (string.IsNullOrWhiteSpace(idCard.Comp.StoredBankAccountNumber))
        {
            idCard.Comp.StoredBankAccountNumber = account.Value.Comp.AccountNumber;
            Dirty(idCard);
        }

        return _bankManager.TryChangeBalanceBy(account.Value, FixedPoint2.New(component.CaptainKillRewardCredits));
    }

    private bool TryCreditSecurityKillReward(
        EntityUid bountyBody,
        MindComponent killerMind,
        EntityUid? sourceEntity,
        TraitorUltraRuleComponent component)
    {
        if (component.SecurityKillRewardCredits <= 0)
            return true;

        var station = _station.GetOwningStation(bountyBody) ??
                      _station.GetOwningStation(sourceEntity) ??
                      _station.GetOwningStation(killerMind.OwnedEntity);

        if (station == null || !TryComp<StationBankAccountComponent>(station.Value, out var bank))
            return false;

        _cargo.UpdateBankAccount((station.Value, bank), component.SecurityKillRewardCredits, component.SecurityRewardAccount);
        return true;
    }

    private EntityUid? GetKillerRewardEntity(MindComponent killerMind, EntityUid? sourceEntity)
    {
        return sourceEntity != null && !TerminatingOrDeleted(sourceEntity.Value)
            ? sourceEntity.Value
            : killerMind.OwnedEntity;
    }

    private string GetRewardAccountName(EntityUid killerEntity, Entity<IdCardComponent> idCard)
    {
        return string.IsNullOrWhiteSpace(idCard.Comp.FullName)
            ? Name(killerEntity)
            : idCard.Comp.FullName;
    }

    private void AddTelecrystals(EntityUid mindId, MindComponent mind, FixedPoint2 amount, TraitorUltraRuleComponent component)
    {
        if (amount <= FixedPoint2.Zero || mind.OwnedEntity is not { } owned)
            return;

        if (component.Minds.TryGetValue(mindId, out var state) ||
            _roles.MindHasRole<TraitorUltraRoleComponent>(mindId))
        {
            state ??= new TraitorUltraMindState();
            AddUltraTelecrystals((mindId, mind), amount, component, state);
            return;
        }

        AddStandardTelecrystals(amount, component, owned);
    }

    private void AddUltraTelecrystals(
        Entity<MindComponent> mind,
        FixedPoint2 amount,
        TraitorUltraRuleComponent component,
        TraitorUltraMindState state)
    {
        var uplink = EnsureUltraUplink(mind, component, state);
        if (uplink == null ||
            mind.Comp.OwnedEntity is not { } owned ||
            !TryComp<StoreComponent>(uplink.Value, out var store))
        {
            return;
        }

        ApplyCorporateDiscounts(store, component, state);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { component.TelecrystalCurrency, amount } }, uplink.Value, store);
        _store.UpdateUserInterface(owned, uplink.Value, store);
    }

    private void AddStandardTelecrystals(
        FixedPoint2 amount,
        TraitorUltraRuleComponent component,
        EntityUid owned)
    {
        var uplink = _uplink.FindUplinkTarget(owned);
        if (uplink == null || !TryComp<StoreComponent>(uplink.Value, out var store))
        {
            _uplink.AddUplink(owned, amount, giveDiscounts: true);
            return;
        }

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { component.TelecrystalCurrency, amount } }, uplink.Value, store);
        _store.UpdateUserInterface(owned, uplink.Value, store);
    }

    private void QueueDelayedAction(TimeSpan delay, EntityUid rule, EntityUid mindId, TraitorUltraDelayedActionType type, string? corporation = null)
    {
        _delayedActions.Add(new TraitorUltraDelayedAction(Timing.CurTime + delay, rule, mindId, type, corporation));
    }

    private void ProcessDelayedActions()
    {
        for (var i = _delayedActions.Count - 1; i >= 0; i--)
        {
            var action = _delayedActions[i];
            if (Timing.CurTime < action.At)
                continue;

            _delayedActions.RemoveAt(i);

            if (!TryComp<TraitorUltraRuleComponent>(action.Rule, out var component))
            {
                continue;
            }

            switch (action.Type)
            {
                case TraitorUltraDelayedActionType.AnnounceBounty:
                    if (component.Minds.TryGetValue(action.MindId, out var state))
                    {
                        TryComp<MindComponent>(action.MindId, out var announceMind);
                        AnnounceBounty(action.Rule, component, action.MindId, announceMind, state);
                    }
                    break;

                case TraitorUltraDelayedActionType.GiveTraitorKillReward:
                    if (TryComp<MindComponent>(action.MindId, out var rewardMind))
                        AddTelecrystals(action.MindId, rewardMind, component.TraitorKillRewardTelecrystals, component);
                    break;

                case TraitorUltraDelayedActionType.OpenRecruitOffer:
                    if (TryComp<MindComponent>(action.MindId, out var recruitMind) &&
                        !_roles.MindIsAntagonist(action.MindId) &&
                        TryGetSession(recruitMind, out var session))
                    {
                        component.PendingRecruitOffers[action.MindId] = action.Corporation;
                        _eui.OpenEui(new TraitorUltraRecruitEui(action.Rule, action.MindId, this, action.Corporation), session);
                    }
                    break;
            }
        }
    }

    private string? GetOriginalCorporation(EntityUid rule, EntityUid mindId)
    {
        if (TryComp<TraitorRuleComponent>(rule, out var traitorRule) &&
            traitorRule.ObjectiveIssuersByMind.TryGetValue(mindId, out var issuer))
        {
            return issuer;
        }

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out _, out traitorRule))
        {
            if (traitorRule.ObjectiveIssuersByMind.TryGetValue(mindId, out issuer))
                return issuer;
        }

        return null;
    }

    private string? PickNewCorporation(TraitorUltraRuleComponent component, string? original, string? current = null)
    {
        if (!_proto.TryIndex(component.CorporationDataset, out var dataset) || dataset.Values.Count == 0)
            return current;

        if (!string.IsNullOrWhiteSpace(current) && !CorporationMatches(current, original))
            return NormalizeCorporation(dataset, current);

        var values = dataset.Values.ToList();
        if (original != null && values.Count > 1)
            values.RemoveAll(value => CorporationMatches(value, original));

        return values.Count == 0
            ? NormalizeCorporation(dataset, current)
            : _random.Pick(values);
    }

    private string? NormalizeCorporation(LocalizedDatasetPrototype dataset, string? corporation)
    {
        if (string.IsNullOrWhiteSpace(corporation))
            return null;

        foreach (var locId in dataset.Values)
        {
            if (CorporationMatches(locId, corporation))
                return locId;
        }

        return corporation;
    }

    private bool CorporationMatches(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(LocalizeCorporation(left), LocalizeCorporation(right), StringComparison.OrdinalIgnoreCase);
    }

    private string LocalizeCorporation(string? locId)
    {
        return string.IsNullOrWhiteSpace(locId)
            ? Loc.GetString("objective-issuer-unknown")
            : Loc.GetString(locId);
    }

    private string GetBountyAnnouncementLocId(string? locId)
    {
        return locId switch
        {
            "traitor-corporations-dataset-1" => "traitor-ultra-bounty-announcement-cybersun",
            "traitor-corporations-dataset-2" => "traitor-ultra-bounty-announcement-gorlex",
            "traitor-corporations-dataset-3" => "traitor-ultra-bounty-announcement-interdyne",
            "traitor-corporations-dataset-7" => "traitor-ultra-bounty-announcement-donk",
            _ => "traitor-ultra-bounty-announcement",
        };
    }

    private string? PickRandomAnnouncementVoice()
    {
        var voices = _proto.EnumeratePrototypes<TTSVoicePrototype>().ToArray();
        return voices.Length == 0 ? null : _random.Pick(voices).ID;
    }

    private string? GetMindCharacterName(MindComponent mind)
    {
        var name = mind.CharacterName;
        if (string.IsNullOrWhiteSpace(name) && mind.OwnedEntity is { } owned)
            name = Name(owned);

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private bool TryGetSession(MindComponent mind, out ICommonSession session)
    {
        session = default!;
        if (mind.UserId == null || !_players.TryGetSessionById(mind.UserId.Value, out var found))
            return false;

        session = found;
        return true;
    }

    private void ShowPopup(MindComponent mind, string locId, PopupType type)
    {
        if (mind.OwnedEntity is { } owned)
            _popup.PopupEntity(Loc.GetString(locId), owned, owned, type);
    }

    private void SendMindMessage(MindComponent mind, string locId, Color color)
    {
        if (!TryGetSession(mind, out var session))
            return;

        var message = Loc.GetString(locId);
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(message)));
        ChatManager.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, session.Channel, color);
    }

    private static bool IsPlayerMind(MindComponent mind)
    {
        return mind.UserId != null || mind.OriginalOwnerUserId != null;
    }
}

public readonly record struct TraitorUltraDelayedAction(
    TimeSpan At,
    EntityUid Rule,
    EntityUid MindId,
    TraitorUltraDelayedActionType Type,
    string? Corporation = null);

public enum TraitorUltraDelayedActionType : byte
{
    AnnounceBounty,
    GiveTraitorKillReward,
    OpenRecruitOffer,
}
