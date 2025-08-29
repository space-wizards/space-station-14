using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Containers;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using Content.Shared.Store.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Cuffs.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Content.Shared.Popups;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Containers;
using Content.Server.Revolutionary;
using Content.Server.Traitor.Uplink;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Implants.Components;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // Starlight
    [Dependency] private readonly ChatSystem _chatSystem = default!; // Starlight
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!; // Starlight
    [Dependency] private readonly SpecialLobbyContentSystem _specialLobbyContent = default!; // Starlight

    //Used in OnPostFlash, no reference to the rule component is available
    public readonly ProtoId<NpcFactionPrototype> RevolutionaryNpcFaction = "Revolutionary";
    public readonly ProtoId<NpcFactionPrototype> RevPrototypeId = "Rev";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);

        SubscribeLocalEvent<RevolutionaryRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<RevolutionaryRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntitySelected); // Starlight
    }

    // Starlight Start
    private void OnAfterAntagEntitySelected(EntityUid uid, RevolutionaryRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        // Send a custom briefing with the character's name
        var name = Identity.Name(args.EntityUid, EntityManager);
        _antag.SendBriefing(args.Session, Loc.GetString("head-rev-role-greeting", ("name", name)), Color.LightYellow, new SoundPathSpecifier("/Audio/Ambience/Antag/headrev_start.ogg"));
    }
    // Starlight End

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }

    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (component.CommandCheck <= _timing.CurTime)
        {
            component.CommandCheck = _timing.CurTime + component.TimerWait;

            //starlight, check if revs have lost
            if (CheckRevsLose())
            {
                GameTicker.EndGameRule(uid, gameRule);
            }

            if (CheckCommandLose())
            {
                // Starlight Start

                _roundEnd.CancelRoundEndCountdown(null, false);

                // Play the revolutionary end sound globally
                var filter = Filter.Broadcast();
                _audioSystem.PlayGlobal("/Audio/_Starlight/Effects/sov_choir_global.ogg", filter, false);

                // First, end the game rule
                GameTicker.EndGameRule(uid, gameRule);

                // Check if the emergency shuttle is already called (not just arrived)
                if (_roundEnd.IsRoundEndRequested())
                {
                    // If the shuttle is already called, we need to recall it
                    // Cancel the current shuttle call - force it with false for checkCooldown
                    _roundEnd.CancelRoundEndCountdown(null, false);
                }

                // Use a safer approach for scheduling the announcements
                // Schedule the first announcement after 7 seconds
                Timer.Spawn(TimeSpan.FromSeconds(7), () =>
                {
                    try
                    {
                        // Send Central Command announcement
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("central-command-revolution-announcement"),
                            Loc.GetString("central-command-sender"),
                            true,
                            new SoundPathSpecifier("/Audio/_Starlight/Announcements/announce_broken.ogg"),
                            Color.Red
                        );

                        // Remove event schedulers
                        RemoveEventSchedulers();
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorS("rev-rule", $"Error during first announcement: {ex}");
                    }
                });

                // Schedule the second announcement separately after 22 seconds (7 + 15)
                Timer.Spawn(TimeSpan.FromSeconds(32), () =>
                {
                    try
                    {
                        // Send Soviet People's Commissariat announcement
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("soviet-commissariat-revolution-announcement"),
                            Loc.GetString("soviet-commissariat-sender"),
                            true,
                            new SoundPathSpecifier("/Audio/_Starlight/Announcements/sov_announce.ogg"),
                            Color.Yellow
                        );

                        // Wait a short time to ensure the announcement is heard before ending the round
                        Timer.Spawn(TimeSpan.FromSeconds(4), () =>
                        {
                            // STARLIGHT: Set special lobby content for revolutionary victory using the modular system
                            _specialLobbyContent.SetSpecialLobbyContent(uid);
                            
                            // End the round
                            // _audioSystem.PlayGlobal("/Audio/_Starlight/Misc/sov_win.ogg", filter, false);
                            _roundEnd.EndRound();
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorS("rev-rule", $"Error during second announcement: {ex}");
                        // Still try to end the round even if the announcement fails
                        _roundEnd.EndRound();
                    }
                });
                // Starlight End
            }
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var revsLost = CheckRevsLose();
        var commandLost = CheckCommandLose();
        // This is (revsLost, commandsLost) concatted together
        // (moony wrote this comment idk what it means)
        var index = (commandLost ? 1 : 0) | (revsLost ? 2 : 0);
        args.AddLine(Loc.GetString(Outcomes[index]));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("rev-headrev-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            _role.MindHasRole<RevolutionaryRoleComponent>(mind, out var role);
            var count = CompOrNull<RevolutionaryRoleComponent>(role)?.ConvertedCount ?? 0;

            args.AddLine(Loc.GetString("rev-headrev-name-user",
                ("name", name),
                ("username", data.UserName),
                ("count", count)));

            // TODO: someone suggested listing all alive? revs maybe implement at some point
        }
    }

    private void OnGetBriefing(EntityUid uid, RevolutionaryRoleComponent comp, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        var head = HasComp<HeadRevolutionaryComponent>(ent);
        args.Append(Loc.GetString(head ? "head-rev-briefing" : "rev-briefing"));
    }


    /// <summary>
    /// STARLIGHT: Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    private EntityUid? FindUSSPUplink(EntityUid user)
    {
        var uplinkSystem = EntityManager.System<UplinkSystem>();
        var inventorySystem = EntityManager.System<InventorySystem>();
        var implantSystem = EntityManager.System<SubdermalImplantSystem>();

        // If this is a head revolutionary, check if we already have a stored implant UID
        if (TryComp<HeadRevolutionaryImplantComponent>(user, out var implantComp) && implantComp.ImplantUid != null)
        {
            // Verify the implant still exists and is valid
            if (EntityManager.EntityExists(implantComp.ImplantUid.Value) &&
                EntityManager.HasComponent<StoreComponent>(implantComp.ImplantUid.Value))
            {
                return implantComp.ImplantUid.Value;
            }
        }

        // Check for USSPUplinkImplant in user's implants
        if (implantSystem.TryGetImplants(user, out var implants))
        {
            foreach (var implant in implants)
            {
                if (EntityManager.HasComponent<StoreComponent>(implant) &&
                    EntityManager.GetComponent<MetaDataComponent>(implant).EntityPrototype?.ID == "USSPUplinkImplant")
                {
                    // Store the implant UID in the head revolutionary implant component for future use
                    if (HasComp<HeadRevolutionaryComponent>(user))
                    {
                        var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(user);
                        implantComponent.ImplantUid = implant;
                    }

                    return implant;
                }
            }
        }

        // Search container slots
        if (inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var slotEntity))
            {
                if (!slotEntity.ContainedEntity.HasValue)
                    continue;

                var contained = slotEntity.ContainedEntity.Value;
                if (EntityManager.HasComponent<StoreComponent>(contained) &&
                    EntityManager.GetComponent<MetaDataComponent>(contained).EntityPrototype?.ID == "USSPUplinkRadioPreset")
                {
                    // Store the uplink UID in the head revolutionary implant component for future use
                    if (HasComp<HeadRevolutionaryComponent>(user))
                    {
                        var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(user);
                        implantComponent.ImplantUid = contained;
                    }

                    return contained;
                }
            }
        }

        // Search held items
        var handsSystem = EntityManager.System<SharedHandsSystem>();
        foreach (var held in handsSystem.EnumerateHeld(user))
        {
            if (EntityManager.HasComponent<StoreComponent>(held) &&
                EntityManager.GetComponent<MetaDataComponent>(held).EntityPrototype?.ID == "USSPUplinkRadioPreset")
            {
                // Store the uplink UID in the head revolutionary implant component for future use
                if (HasComp<HeadRevolutionaryComponent>(user))
                {
                    var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(user);
                    implantComponent.ImplantUid = held;
                }

                return held;
            }
        }

        return null;
    }
    // STARLIGHT END

    private void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        if (uid != ev.User || !ev.Melee)
            return;

        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target);

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidAppearanceComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, RevolutionaryNpcFaction);
        var revComp = EnsureComp<RevolutionaryComponent>(ev.Target);

        // Starlight: Add a component to track which head revolutionary converted this revolutionary
        if (ev.User != null && HasComp<HeadRevolutionaryComponent>(ev.User.Value))
        {
            var converterComp = EnsureComp<RevolutionaryConverterComponent>(ev.Target);
            converterComp.ConverterUid = ev.User.Value;
        }
        // Starlight End

        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");

            // STARLIGHT START
            var storeSystem = EntityManager.System<StoreSystem>();

            // Add Telebond to the converter's uplink
            if (HasComp<HeadRevolutionaryComponent>(ev.User.Value))
            {
                // Find the head revolutionary's uplink
                var uplinkUid = FindUSSPUplink(ev.User.Value);

                // If no uplink was found, create one
                if (uplinkUid == null)
                {
                    // Create a new USSP uplink implant for this head revolutionary
                    var uplinkImplant = EntityManager.SpawnEntity("USSPUplinkImplant", Transform(ev.User.Value).Coordinates);
                    uplinkUid = uplinkImplant;

                    // Store this uplink for future use
                    var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(ev.User.Value);
                    implantComponent.ImplantUid = uplinkImplant;

                    // Add a component to the uplink to track which head revolutionary it belongs to
                    var uplinkOwnerComp = EnsureComp<USSPUplinkOwnerComponent>(uplinkImplant);
                    uplinkOwnerComp.OwnerUid = ev.User.Value;
                }

                // Add Telebond to the uplink
                if (uplinkUid != null)
                {
                    // Debug log to see the current telebond value
                    if (TryComp<StoreComponent>(uplinkUid.Value, out var storeComp))
                    {
                        var currentTelebond = storeComp.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    }

                    // Ensure the uplink has an owner component that points to this head revolutionary
                    var uplinkOwnerComp = EnsureComp<USSPUplinkOwnerComponent>(uplinkUid.Value);
                    uplinkOwnerComp.OwnerUid = ev.User.Value;

                    var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Telebond", FixedPoint2.New(1) } };
                    var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkUid.Value);

                    // Debug log to see the updated telebond value
                    if (TryComp<StoreComponent>(uplinkUid.Value, out var storeCompAfter))
                    {
                        var updatedTelebond = storeCompAfter.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    }

                    // Make sure the head revolutionary's implant component points to this uplink
                    var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(ev.User.Value);
                    implantComponent.ImplantUid = uplinkUid;

                    // Synchronize this currency with all other uplinks owned by this head revolutionary
                    SynchronizeUplinkCurrencies(ev.User.Value, uplinkUid.Value);

                    // Also synchronize all uplinks that have this head revolutionary as their owner
                    SynchronizeAllUplinksByOwner(ev.User.Value);

                    // Also directly synchronize all revolutionaries' uplinks with this head revolutionary's uplink
                    var ussplinkSystem = EntitySystem.Get<USSPUplinkSystem>();
                    var revQuery2 = EntityManager.EntityQuery<RevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                    foreach (var (_, revImplantComp) in revQuery2)
                    {
                        if (revImplantComp.ImplantUid != null &&
                            EntityManager.EntityExists(revImplantComp.ImplantUid.Value) &&
                            revImplantComp.ImplantUid.Value != uplinkUid.Value)
                        {
                            // Use the USSPUplinkSystem's SyncUplinkCurrencies method to directly sync the currencies
                            ussplinkSystem.SyncUplinkCurrencies(uplinkUid.Value, revImplantComp.ImplantUid.Value);
                        }
                    }

                    // Get the final telebond value after synchronization
                    var finalTelebond = FixedPoint2.Zero;
                    if (TryComp<StoreComponent>(uplinkUid.Value, out var finalStoreComp))
                    {
                        finalTelebond = finalStoreComp.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                    }

                    // Show popup to the head revolutionary (private)
                    _popup.PopupEntity(Loc.GetString($"+1 Telebond (Total: {finalTelebond})"), ev.User.Value, ev.User.Value, PopupType.Medium);

                    // If the uplink is implanted in someone else, show them a popup too
                    if (TryComp<SubdermalImplantComponent>(uplinkUid.Value, out var implant) &&
                        implant.ImplantedEntity != null &&
                        implant.ImplantedEntity.Value != ev.User.Value)
                    {
                        _popup.PopupEntity(Loc.GetString($"+1 Telebond (Total: {finalTelebond}) (for {Identity.Name(ev.User.Value, EntityManager)})"),
                            implant.ImplantedEntity.Value, implant.ImplantedEntity.Value, PopupType.Large);
                    }

                    // Also show a popup to any revolutionary who has this uplink's entity UID stored in their HeadRevolutionaryImplantComponent
                    var revQuery = EntityManager.EntityQuery<RevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                    foreach (var (_, revImplantComp) in revQuery)
                    {
                        if (revImplantComp.ImplantUid == uplinkUid &&
                            revImplantComp.Owner != ev.User.Value &&
                            (implant == null || implant.ImplantedEntity == null || revImplantComp.Owner != implant.ImplantedEntity.Value))
                        {
                            _popup.PopupEntity(Loc.GetString($"+1 Telebond (for {Identity.Name(ev.User.Value, EntityManager)})"),
                                revImplantComp.Owner, revImplantComp.Owner, PopupType.Large);
                        }
                    }

                    // Also check for any revolutionaries who have an implant with this uplink
                    var allRevs = EntityManager.EntityQuery<RevolutionaryComponent>();
                    foreach (var rev in allRevs)
                    {
                        // Skip the head revolutionary who did the conversion
                        if (rev.Owner == ev.User.Value)
                            continue;

                        // Skip the implanted entity if we already showed them a popup
                        if (implant != null && implant.ImplantedEntity != null && rev.Owner == implant.ImplantedEntity.Value)
                            continue;

                        // Check if this revolutionary has an implant
                        var implantSystem = EntitySystem.Get<SubdermalImplantSystem>();
                        if (implantSystem.TryGetImplants(rev.Owner, out var implants))
                        {
                            foreach (var revImplant in implants)
                            {
                                // Check if this implant is the same as the uplink or has the same owner
                                if (revImplant == uplinkUid ||
                                    (TryComp<USSPUplinkOwnerComponent>(revImplant, out var ownerComp) &&
                                     ownerComp.OwnerUid == ev.User.Value))
                                {
                                    _popup.PopupEntity(Loc.GetString($"+1 Telebond (for {Identity.Name(ev.User.Value, EntityManager)})"),
                                        rev.Owner, rev.Owner, PopupType.Medium);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Add Conversion to ALL head revolutionary uplinks with a 1-second delay
            // This prevents the Conversion popup from appearing at the same time as the Telebond popup
            var uplinkSystem = EntitySystem.Get<USSPUplinkSystem>();
            Timer.Spawn(TimeSpan.FromSeconds(1), () =>
            {
                uplinkSystem.AddConversionToAllHeadRevs(storeSystem);

                // Synchronize all uplinks again to ensure the conversion value is updated everywhere
                uplinkSystem.SynchronizeAllUplinks();
            });

            // STARLIGHT END

            if (_mind.TryGetMind(ev.User.Value, out var revMindId, out _))
            {
                if (_role.MindHasRole<RevolutionaryRoleComponent>(revMindId, out var role))
                {
                    role.Value.Comp2.ConvertedCount++;
                    Dirty(role.Value.Owner, role.Value.Comp2);
                }
            }
        }

        if (mindId == default || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleRevolutionary");
        }

        if (mind?.UserId != null && _player.TryGetSessionById(mind.UserId.Value, out var session))
            _antag.SendBriefing(session, Loc.GetString("rev-role-greeting", ("name", Identity.Name(ev.Target, EntityManager))), Color.LightYellow, revComp.RevStartSound); // STARLIGHT
    }

    //TODO: Enemies of the revolution
    private void OnCommandMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose();
    }

    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    private bool CheckCommandLose()
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }
    
    // STARLIGHT START
        var allCommandDead = IsGroupDetainedOrDead(commandList, true, true, true);
        return allCommandDead;
    }

    /// <summary>
    /// Removes various event schedulers from the game rules.
    /// </summary>
    private void RemoveEventSchedulers()
    {
        // Remove BasicStationEventScheduler
        var basicSchedulers = EntityManager.EntityQuery<BasicStationEventSchedulerComponent>();
        foreach (var scheduler in basicSchedulers)
        {
            EntityManager.RemoveComponent<BasicStationEventSchedulerComponent>(scheduler.Owner);
        }

        // Remove RampingStationEventScheduler
        var rampingSchedulers = EntityManager.EntityQuery<RampingStationEventSchedulerComponent>();
        foreach (var scheduler in rampingSchedulers)
        {
            EntityManager.RemoveComponent<RampingStationEventSchedulerComponent>(scheduler.Owner);
        }

        // Get all game rule entities
        // var gameRuleQuery = EntityManager.EntityQuery<GameRuleComponent>();
        // STARLIGHT END
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose();
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var uid, out _, out _))
        {
            headRevList.Add(uid);
        }

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        // Cuffing Head Revs is not enough - they must be killed.
        if (IsGroupDetainedOrDead(headRevList, false, false, false))
        {
            // STARLIGHT: Delete all USSP uplinks and turn supply rifts and SKB implanters to ash
            DeleteUplinksTurnItemsToAsh();
            
            var rev = AllEntityQuery<RevolutionaryComponent, MindContainerComponent>();
            while (rev.MoveNext(out var uid, out _, out var mc))
            {
                if (HasComp<HeadRevolutionaryComponent>(uid))
                    continue;

                // Play the deconversion sound for the revolutionary
                _audioSystem.PlayGlobal("/Audio/_Starlight/Misc/rev_end.ogg", Filter.Entities(uid), false, AudioParams.Default.WithVolume(0f));
                
                _npcFaction.RemoveFaction(uid, RevolutionaryNpcFaction);
                _stun.TryUpdateParalyzeDuration(uid, stunTime);
                RemCompDeferred<RevolutionaryComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Name(uid, EntityManager))), uid); //STARLIGHT
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

                if (!_mind.TryGetMind(uid, out var mindId, out var mind, mc))
                    continue;

                // remove their antag role
                _role.MindRemoveRole<RevolutionaryRoleComponent>(mindId);

                // make it very obvious to the rev they've been deconverted since
                // they may not see the popup due to antag and/or new player tunnel vision
                if (_player.TryGetSessionById(mind.UserId, out var session))
                    _euiMan.OpenEui(new DeconvertedEui(), session);
            }
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// STARLIGHT: Deletes all USSP uplinks and turns supply rifts and SKB implanters to ash when all head revolutionaries are dead.
    /// </summary>
    private void DeleteUplinksTurnItemsToAsh()
    {
        // Find and delete all USSP uplinks
        var uplinkQuery = EntityManager.EntityQuery<MetaDataComponent>(true);
        var uplinksToDelete = new List<EntityUid>();
        
        foreach (var metadata in uplinkQuery)
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplant")
            {
                uplinksToDelete.Add(metadata.Owner);
            }
        }
        
        // Delete all uplinks
        foreach (var uplink in uplinksToDelete)
        {
            if (EntityManager.EntityExists(uplink))
            {
                EntityManager.QueueDeleteEntity(uplink);
            }
        }
        
        // Find all supply rifts and collect them for deletion
        var riftsToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var riftQuery = EntityManager.EntityQuery<RevSupplyRiftComponent, TransformComponent>();
        
        foreach (var (rift, transform) in riftQuery)
        {
            riftsToDelete.Add((rift.Owner, transform.Coordinates));
        }
        
        // Process all supply rifts
        foreach (var (entity, coordinates) in riftsToDelete)
        {
            if (EntityManager.EntityExists(entity))
            {
                // Spawn ash at the rift's location
                EntityManager.SpawnEntity("Ash", coordinates);
                
                // Delete the rift
                EntityManager.QueueDeleteEntity(entity);
            }
        }
        
        // Find all SKB implanters and collect them for deletion
        var implantersToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var implanterQuery = EntityManager.EntityQuery<MetaDataComponent, TransformComponent>(true);
        
        foreach (var (metadata, transform) in implanterQuery)
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplanter")
            {
                implantersToDelete.Add((metadata.Owner, transform.Coordinates));
            }
        }
        
        // Process all SKB implanters
        foreach (var (entity, coordinates) in implantersToDelete)
        {
            if (EntityManager.EntityExists(entity))
            {
                // Spawn ash at the implanter's location
                EntityManager.SpawnEntity("Ash", coordinates);
                
                // Delete the implanter
                EntityManager.QueueDeleteEntity(entity);
            }
        }
    }

    /// <summary>
    /// Will take a group of entities and check if these entities are alive, dead or cuffed.
    /// </summary>
    /// <param name="list">The list of the entities</param>
    /// <param name="checkOffStation">Bool for if you want to check if someone is in space and consider them missing in action. (Won't check when emergency shuttle arrives just in case)</param>
    /// <param name="countCuffed">Bool for if you don't want to count cuffed entities.</param>
    /// <param name="countRevolutionaries">Bool for if you want to count revolutionaries.</param>
    /// <returns></returns>
    private bool IsGroupDetainedOrDead(List<EntityUid> list, bool checkOffStation, bool countCuffed, bool countRevolutionaries)
    {
        var gone = 0;

        foreach (var entity in list)
        {
            if (TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0 && countCuffed)
            {
                gone++;
                continue;
            }

            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    gone++;
                    continue;
                }

                if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    gone++;
                    continue;
                }
            }
            //If they don't have the MobStateComponent they might as well be dead.
            else
            {
                gone++;
                continue;
            }

            if ((HasComp<RevolutionaryComponent>(entity) || HasComp<HeadRevolutionaryComponent>(entity)) && countRevolutionaries)
            {
                gone++;
                continue;
            }
        }

        return gone == list.Count || list.Count == 0;
    }

    private static readonly string[] Outcomes =
    {
        // revs survived and heads survived... how
        "rev-reverse-stalemate",
        // revs won and heads died
        "rev-won",
        // revs lost and heads survived
        "rev-lost",
        // revs lost and heads died
        "rev-stalemate"
    };
    
    /// <summary>
    /// STARLIGHT: Synchronizes currencies between all uplinks owned by the same head revolutionary.
    /// This ensures that all uplinks have the same amount of telebonds and conversions.
    /// </summary>
    private void SynchronizeUplinkCurrencies(EntityUid headRevUid, EntityUid currentUplinkUid)
    {
        // Find all uplinks owned by this head revolutionary
        var allUplinks = new List<EntityUid>();
        var uplinkQuery = EntityManager.EntityQuery<USSPUplinkOwnerComponent, StoreComponent>();
        
        // Get the current uplink's currencies
        FixedPoint2 currentTelebond = FixedPoint2.Zero;
        FixedPoint2 currentConversion = FixedPoint2.Zero;
        
        if (TryComp<StoreComponent>(currentUplinkUid, out var currentStore))
        {
            currentTelebond = currentStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
            currentConversion = currentStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
        }
        
        // Find all uplinks owned by this head revolutionary and get the maximum currency values
        foreach (var (uplinkOwner, uplinkStore) in uplinkQuery)
        {
            if (uplinkOwner.OwnerUid == headRevUid)
            {
                allUplinks.Add(uplinkOwner.Owner);
                
                // Find the maximum value for each currency across all uplinks
                var telebonds = uplinkStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                var conversions = uplinkStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                
                if (telebonds > currentTelebond)
                {
                    currentTelebond = telebonds;
                }
                
                if (conversions > currentConversion)
                {
                    currentConversion = conversions;
                }
            }
        }
        
        // Now update all uplinks with the maximum values
        foreach (var uplink in allUplinks)
        {
            if (TryComp<StoreComponent>(uplink, out var store))
            {
                // Make sure the store has both currencies initialized
                if (!store.Balance.ContainsKey("Telebond"))
                {
                    store.Balance["Telebond"] = FixedPoint2.Zero;
                }
                
                if (!store.Balance.ContainsKey("Conversion"))
                {
                    store.Balance["Conversion"] = FixedPoint2.Zero;
                }
                
                // Update the currencies if they're lower than the maximum
                if (store.Balance["Telebond"] < currentTelebond)
                {
                    store.Balance["Telebond"] = currentTelebond;
                }
                
                if (store.Balance["Conversion"] < currentConversion)
                {
                    store.Balance["Conversion"] = currentConversion;
                }
            }
        }
    }
    
    /// <summary>
    /// Synchronizes all uplinks that have a specific head revolutionary as their owner.
    /// This ensures that when a head revolutionary earns telebonds, all uplinks owned by them are updated.
    /// </summary>
    public void SynchronizeAllUplinksByOwner(EntityUid headRevUid)
    {        
        // Find all uplinks owned by this head revolutionary
        var allUplinks = new List<EntityUid>();
        var maxTelebond = FixedPoint2.Zero;
        var maxConversion = FixedPoint2.Zero;
        
        // First, check if this head revolutionary has an implant component
        if (TryComp<HeadRevolutionaryImplantComponent>(headRevUid, out var headRevImplant) && 
            headRevImplant.ImplantUid != null && 
            EntityManager.EntityExists(headRevImplant.ImplantUid.Value))
        {
            var headRevUplinkUid = headRevImplant.ImplantUid.Value;
            allUplinks.Add(headRevUplinkUid);
            
            // Get the currency values from the head revolutionary's uplink
            if (TryComp<StoreComponent>(headRevUplinkUid, out var headRevStore))
            {
                maxTelebond = headRevStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                maxConversion = headRevStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
            }
        }
        
        // Find all uplinks that have this head revolutionary as their owner
        var uplinkQuery = EntityManager.EntityQuery<USSPUplinkOwnerComponent, StoreComponent>();
        foreach (var (uplinkOwner, uplinkStore) in uplinkQuery)
        {
            if (uplinkOwner.OwnerUid == headRevUid && !allUplinks.Contains(uplinkOwner.Owner))
            {
                allUplinks.Add(uplinkOwner.Owner);
                
                // Get the currency values
                var telebonds = uplinkStore.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                var conversions = uplinkStore.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                
                // Update the maximum values
                if (telebonds > maxTelebond)
                {
                    maxTelebond = telebonds;
                }
                
                if (conversions > maxConversion)
                {
                    maxConversion = conversions;
                }
            }
        }
        
        // Also check all revolutionaries who have this head revolutionary's uplink
        var revQuery = EntityManager.EntityQuery<RevolutionaryComponent, HeadRevolutionaryImplantComponent>();
        foreach (var (_, revImplant) in revQuery)
        {
            if (revImplant.ImplantUid != null && 
                EntityManager.EntityExists(revImplant.ImplantUid.Value) &&
                !allUplinks.Contains(revImplant.ImplantUid.Value))
            {
                // Check if this uplink is owned by the head revolutionary
                if (TryComp<USSPUplinkOwnerComponent>(revImplant.ImplantUid.Value, out var uplinkOwner) && 
                    uplinkOwner.OwnerUid == headRevUid)
                {
                    allUplinks.Add(revImplant.ImplantUid.Value);
                    
                    // Get the currency values
                    if (TryComp<StoreComponent>(revImplant.ImplantUid.Value, out var store))
                    {
                        var telebonds = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                        var conversions = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                                
                        // Update the maximum values
                        if (telebonds > maxTelebond)
                        {
                            maxTelebond = telebonds;
                        }
                        
                        if (conversions > maxConversion)
                        {
                            maxConversion = conversions;
                        }
                    }
                }
            }
        }
        
        // Check all revolutionaries for implants that might be owned by this head revolutionary
        var allRevs = EntityManager.EntityQuery<RevolutionaryComponent>();
        foreach (var rev in allRevs)
        {
            // Skip the head revolutionary
            if (rev.Owner == headRevUid)
                continue;
                
            // Check if this revolutionary has implants
            var implantSystem = EntitySystem.Get<SubdermalImplantSystem>();
            if (implantSystem.TryGetImplants(rev.Owner, out var implants))
            {
                foreach (var implant in implants)
                {
                    // Skip implants we've already processed
                    if (allUplinks.Contains(implant))
                        continue;
                        
                    // Check if this implant is owned by the head revolutionary
                    if (TryComp<USSPUplinkOwnerComponent>(implant, out var ownerComp) && 
                        ownerComp.OwnerUid == headRevUid)
                    {
                        allUplinks.Add(implant);
                        
                        // Get the currency values
                        if (TryComp<StoreComponent>(implant, out var store))
                        {
                            var telebonds = store.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                            var conversions = store.Balance.GetValueOrDefault("Conversion", FixedPoint2.Zero);
                                                        
                            // Update the maximum values
                            if (telebonds > maxTelebond)
                            {
                                maxTelebond = telebonds;
                            }
                            
                            if (conversions > maxConversion)
                            {
                                maxConversion = conversions;
                            }
                        }
                    }
                }
            }
        }
        
        // Also update the global conversion value for all USSP uplinks in the game
        // This ensures that all uplinks have the same conversion value, regardless of owner
        var allUplinkQuery = EntityManager.EntityQuery<MetaDataComponent, StoreComponent>();
        foreach (var (metadata, uplinkStore) in allUplinkQuery)
        {
            // Skip uplinks we've already processed
            if (allUplinks.Contains(metadata.Owner))
                continue;
                
            // Only process USSP uplink implants, not PDAs or other store components
            if (metadata.EntityPrototype?.ID != "USSPUplinkImplant")
                continue;
                
            // Make sure the store has the Conversion currency initialized
            if (!uplinkStore.Balance.ContainsKey("Conversion"))
            {
                uplinkStore.Balance["Conversion"] = FixedPoint2.Zero;
            }
            
            // Update the Conversion currency if it's lower than the maximum
            if (uplinkStore.Balance["Conversion"] < maxConversion)
            {
                uplinkStore.Balance["Conversion"] = maxConversion;
            }
            
            // Check if this uplink has a higher Conversion value
            if (uplinkStore.Balance["Conversion"] > maxConversion)
            {
                maxConversion = uplinkStore.Balance["Conversion"];
                
                // Update all uplinks we've already processed with this higher value
                foreach (var processedUplink in allUplinks)
                {
                    if (TryComp<StoreComponent>(processedUplink, out var processedStore))
                    {
                        processedStore.Balance["Conversion"] = maxConversion;
                    }
                }
            }
        }
        
        // Don't call USSPUplinkSystem.SynchronizeAllUplinks here to avoid stack overflow
        // The USSPUplinkSystem will call this method for each head revolutionary
    }
    
    /// <summary>
    /// Adds Conversion currency to all head revolutionary uplinks.
    /// This is a shared counter that tracks total conversions by all head revolutionaries.
    /// </summary>
    private void AddConversionToAllHeadRevs(StoreSystem storeSystem)
    {        
        // Get all USSPUplinkImplant entities in the game
        var query = EntityManager.EntityQuery<MetaDataComponent, StoreComponent>(true);
        var uplinkEntities = new List<EntityUid>();
        
        foreach (var (metadata, _) in query)
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplant")
            {
                uplinkEntities.Add(metadata.Owner);
            }
        }
        
        // If no uplinks were found, log a warning
        if (uplinkEntities.Count == 0)
        {
            return;
        }
        
        // Add Conversion to all uplinks
        foreach (var uplinkEntity in uplinkEntities)
        {
            var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } };
            var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkEntity);
        }
        
        // Show popup to all head revolutionaries (private)
        var headRevs = AllEntityQuery<HeadRevolutionaryComponent>();
        while (headRevs.MoveNext(out var headRevUid, out _))
        {
            _popup.PopupEntity(Loc.GetString("+1 Conversion"), headRevUid, headRevUid, PopupType.Medium);
        }
    }
}
