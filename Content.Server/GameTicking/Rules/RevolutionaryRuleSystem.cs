using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Containers;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.Inventory; // Added this line for InventorySystem
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
using Content.Shared.Database;
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
using Content.Shared.Implants.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ShuttleBuildingUplinkSystem _shuttleUplink = default!;

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
        SubscribeLocalEvent<RevolutionaryRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntitySelected);
    }

    private void OnAfterAntagEntitySelected(EntityUid uid, RevolutionaryRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        // Check if this is a head revolutionary
        if (!HasComp<HeadRevolutionaryComponent>(args.EntityUid) || args.Session == null)
            return;

        // Create a USSP uplink implant for this head revolutionary
        var uplinkImplant = EntityManager.SpawnEntity("USSPUplinkImplant", Transform(args.EntityUid).Coordinates);
        
        // Store this uplink for future use
        var implantComponent = EnsureComp<HeadRevolutionaryImplantComponent>(args.EntityUid);
        implantComponent.ImplantUid = uplinkImplant;
        
        // Add a component to the uplink to track which head revolutionary it belongs to
        var uplinkOwnerComp = EnsureComp<USSPUplinkOwnerComponent>(uplinkImplant);
        uplinkOwnerComp.OwnerUid = args.EntityUid;
        
        // Initialize the uplink with zero currencies
        if (TryComp<StoreComponent>(uplinkImplant, out var store))
        {
            var storeSystem = EntitySystem.Get<StoreSystem>();
            var currencyToAdd = new Dictionary<string, FixedPoint2> 
            { 
                { "Telebond", FixedPoint2.Zero },
                { "Conversion", FixedPoint2.Zero }
            };
            storeSystem.TryAddCurrency(currencyToAdd, uplinkImplant);
            Logger.InfoS("rev-rule", $"Created new uplink {ToPrettyString(uplinkImplant)} for head revolutionary {ToPrettyString(args.EntityUid)}");
            Logger.InfoS("rev-rule", $"Added USSPUplinkOwnerComponent to uplink {ToPrettyString(uplinkImplant)} with owner {ToPrettyString(args.EntityUid)}");
        }

        // Send a custom briefing with the character's name
        var name = Identity.Name(args.EntityUid, EntityManager);
        _antag.SendBriefing(args.Session, Loc.GetString("head-rev-role-greeting", ("name", name)), Color.LightYellow, new SoundPathSpecifier("/Audio/Ambience/Antag/headrev_start.ogg"));
    }

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

            if (CheckCommandLose())
            {
                _roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, component.ShuttleCallTime);
                GameTicker.EndGameRule(uid, gameRule);
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
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
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

    private void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target);

        Logger.Info($"OnPostFlash called: User={ev.User}, Target={ev.Target}, AlwaysConvertible={alwaysConvertible}");

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidAppearanceComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            Logger.Info("OnPostFlash: Target failed conversion checks");
            return;
        }

        _npcFaction.AddFaction(ev.Target, RevolutionaryNpcFaction);
        var revComp = EnsureComp<RevolutionaryComponent>(ev.Target);

        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");

            Logger.Info($"OnPostFlash: Granting currency to user {ev.User.Value}");

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
                        
                        Logger.Info($"OnPostFlash: Created new uplink {ToPrettyString(uplinkImplant)} for head revolutionary {ToPrettyString(ev.User.Value)}");
                        Logger.Info($"OnPostFlash: Added USSPUplinkOwnerComponent to uplink {ToPrettyString(uplinkImplant)} with owner {ToPrettyString(ev.User.Value)}");
                    }
                    
                    // Add Telebond to the uplink
                    if (uplinkUid != null)
                    {
                        // Debug log to see the current telebond value
                        if (TryComp<StoreComponent>(uplinkUid.Value, out var storeComp))
                        {
                            var currentTelebond = storeComp.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                            Logger.Info($"OnPostFlash: Current Telebond before adding: {currentTelebond}");
                        }
                        
                        // Ensure the uplink has an owner component that points to this head revolutionary
                        var uplinkOwnerComp = EnsureComp<USSPUplinkOwnerComponent>(uplinkUid.Value);
                        uplinkOwnerComp.OwnerUid = ev.User.Value;
                        Logger.Info($"OnPostFlash: Ensured uplink {ToPrettyString(uplinkUid.Value)} has owner {ToPrettyString(ev.User.Value)}");
                        
                        var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Telebond", FixedPoint2.New(1) } };
                        var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkUid.Value);
                        Logger.Info($"OnPostFlash: Added Telebond to uplink {ToPrettyString(uplinkUid.Value)}, success: {success}");
                        
                        // Debug log to see the updated telebond value
                        if (TryComp<StoreComponent>(uplinkUid.Value, out var storeCompAfter))
                        {
                            var updatedTelebond = storeCompAfter.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);
                            Logger.Info($"OnPostFlash: Current Telebond after adding: {updatedTelebond}");
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
                                            rev.Owner, rev.Owner, PopupType.Large);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            
            // Add Conversion to ALL head revolutionary uplinks
            var uplinkSystem = EntitySystem.Get<USSPUplinkSystem>();
            uplinkSystem.AddConversionToAllHeadRevs(storeSystem);
            
            // Synchronize all uplinks again to ensure the conversion value is updated everywhere
            uplinkSystem.SynchronizeAllUplinks();

            if (_mind.TryGetMind(ev.User.Value, out var revMindId, out _))
            {
                if (_role.MindHasRole<RevolutionaryRoleComponent>(revMindId, out var role))
                    role.Value.Comp2.ConvertedCount++;
            }
        }

        if (mindId == default || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleRevolutionary");
        }

        if (mind?.Session != null)
            _antag.SendBriefing(mind.Session, Loc.GetString("rev-role-greeting", ("name", Identity.Name(ev.Target, EntityManager))), Color.LightYellow, revComp.RevStartSound);
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

        return IsGroupDetainedOrDead(commandList, true, true, true);
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
            // Delete all USSP uplinks and turn supply rifts and SNKVD implanters to ash
            DeleteUplinksTurnItemsToAsh();
            
            var rev = AllEntityQuery<RevolutionaryComponent, MindContainerComponent>();
            while (rev.MoveNext(out var uid, out _, out var mc))
            {
                if (HasComp<HeadRevolutionaryComponent>(uid))
                    continue;

                _npcFaction.RemoveFaction(uid, RevolutionaryNpcFaction);
                _stun.TryParalyze(uid, stunTime, true);
                RemCompDeferred<RevolutionaryComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Name(uid, EntityManager))), uid);
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

                if (!_mind.TryGetMind(uid, out var mindId, out _, mc))
                    continue;

                // remove their antag role
                _role.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);

                // make it very obvious to the rev they've been deconverted since
                // they may not see the popup due to antag and/or new player tunnel vision
                if (_mind.TryGetSession(mindId, out var session))
                    _euiMan.OpenEui(new DeconvertedEui(), session);
            }
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Deletes all USSP uplinks and turns supply rifts and SNKVD implanters to ash when all head revolutionaries are dead.
    /// </summary>
    private void DeleteUplinksTurnItemsToAsh()
    {
        Logger.InfoS("rev-rule", "All head revolutionaries are dead. Deleting uplinks and turning items to ash.");
        
        // Find and delete all USSP uplinks
        var uplinkQuery = EntityManager.EntityQuery<MetaDataComponent>(true);
        var uplinksToDelete = new List<EntityUid>();
        
        foreach (var metadata in uplinkQuery)
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplant")
            {
                uplinksToDelete.Add(metadata.Owner);
                Logger.InfoS("rev-rule", $"Found USSP uplink to delete: {ToPrettyString(metadata.Owner)}");
            }
        }
        
        // Delete all uplinks
        foreach (var uplink in uplinksToDelete)
        {
            if (EntityManager.EntityExists(uplink))
            {
                EntityManager.DeleteEntity(uplink);
                Logger.InfoS("rev-rule", $"Deleted USSP uplink: {ToPrettyString(uplink)}");
            }
        }
        
        // Find all supply rifts and collect them for deletion
        var riftsToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var riftQuery = EntityManager.EntityQuery<RevSupplyRiftComponent, TransformComponent>();
        
        foreach (var (rift, transform) in riftQuery)
        {
            riftsToDelete.Add((rift.Owner, transform.Coordinates));
            Logger.InfoS("rev-rule", $"Found supply rift to turn to ash: {ToPrettyString(rift.Owner)}");
        }
        
        // Process all supply rifts
        foreach (var (entity, coordinates) in riftsToDelete)
        {
            if (EntityManager.EntityExists(entity))
            {
                // Spawn ash at the rift's location
                EntityManager.SpawnEntity("Ash", coordinates);
                Logger.InfoS("rev-rule", $"Turned supply rift to ash: {ToPrettyString(entity)}");
                
                // Delete the rift
                EntityManager.DeleteEntity(entity);
            }
        }
        
        // Find all SNKVD implanters and collect them for deletion
        var implantersToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var implanterQuery = EntityManager.EntityQuery<MetaDataComponent, TransformComponent>(true);
        
        foreach (var (metadata, transform) in implanterQuery)
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplanter")
            {
                implantersToDelete.Add((metadata.Owner, transform.Coordinates));
                Logger.InfoS("rev-rule", $"Found SNKVD implanter to turn to ash: {ToPrettyString(metadata.Owner)}");
            }
        }
        
        // Process all SNKVD implanters
        foreach (var (entity, coordinates) in implantersToDelete)
        {
            if (EntityManager.EntityExists(entity))
            {
                // Spawn ash at the implanter's location
                EntityManager.SpawnEntity("Ash", coordinates);
                Logger.InfoS("rev-rule", $"Turned SNKVD implanter to ash: {ToPrettyString(entity)}");
                
                // Delete the implanter
                EntityManager.DeleteEntity(entity);
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
    /// Synchronizes currencies between all uplinks owned by the same head revolutionary.
    /// This ensures that all uplinks have the same amount of telebonds and conversions.
    /// </summary>
    private void SynchronizeUplinkCurrencies(EntityUid headRevUid, EntityUid currentUplinkUid)
    {
        Logger.Info($"Synchronizing uplink currencies for head revolutionary {ToPrettyString(headRevUid)}");
        
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
                    Logger.Info($"Found higher Telebond value ({telebonds}) in uplink {ToPrettyString(uplinkOwner.Owner)}");
                }
                
                if (conversions > currentConversion)
                {
                    currentConversion = conversions;
                    Logger.Info($"Found higher Conversion value ({conversions}) in uplink {ToPrettyString(uplinkOwner.Owner)}");
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
                    Logger.Info($"Updated Telebond currency in uplink {ToPrettyString(uplink)} to {currentTelebond}");
                }
                
                if (store.Balance["Conversion"] < currentConversion)
                {
                    store.Balance["Conversion"] = currentConversion;
                    Logger.Info($"Updated Conversion currency in uplink {ToPrettyString(uplink)} to {currentConversion}");
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
        Logger.Info($"Synchronizing all uplinks owned by head revolutionary {ToPrettyString(headRevUid)}");
        
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
                
                Logger.Info($"Found head revolutionary's uplink {ToPrettyString(headRevUplinkUid)} with Telebond: {maxTelebond}, Conversion: {maxConversion}");
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
                
                Logger.Info($"Found uplink {ToPrettyString(uplinkOwner.Owner)} with Telebond: {telebonds}, Conversion: {conversions}");
                
                // Update the maximum values
                if (telebonds > maxTelebond)
                {
                    maxTelebond = telebonds;
                    Logger.Info($"Found higher Telebond value: {telebonds}");
                }
                
                if (conversions > maxConversion)
                {
                    maxConversion = conversions;
                    Logger.Info($"Found higher Conversion value: {conversions}");
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
                        
                        Logger.Info($"Found revolutionary's uplink {ToPrettyString(revImplant.ImplantUid.Value)} with Telebond: {telebonds}, Conversion: {conversions}");
                        
                        // Update the maximum values
                        if (telebonds > maxTelebond)
                        {
                            maxTelebond = telebonds;
                            Logger.Info($"Found higher Telebond value: {telebonds}");
                        }
                        
                        if (conversions > maxConversion)
                        {
                            maxConversion = conversions;
                            Logger.Info($"Found higher Conversion value: {conversions}");
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
                            
                            Logger.Info($"Found implanted uplink {ToPrettyString(implant)} in revolutionary {ToPrettyString(rev.Owner)} with Telebond: {telebonds}, Conversion: {conversions}");
                            
                            // Update the maximum values
                            if (telebonds > maxTelebond)
                            {
                                maxTelebond = telebonds;
                                Logger.Info($"Found higher Telebond value: {telebonds}");
                            }
                            
                            if (conversions > maxConversion)
                            {
                                maxConversion = conversions;
                                Logger.Info($"Found higher Conversion value: {conversions}");
                            }
                        }
                    }
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
                if (store.Balance["Telebond"] < maxTelebond)
                {
                    store.Balance["Telebond"] = maxTelebond;
                    Logger.Info($"Updated Telebond currency in uplink {ToPrettyString(uplink)} to {maxTelebond}");
                    
                    // Show popup to the implanted entity
                    if (TryComp<SubdermalImplantComponent>(uplink, out var implant) && 
                        implant.ImplantedEntity != null && 
                        implant.ImplantedEntity.Value != headRevUid)
                    {
                        _popup.PopupEntity(Loc.GetString($"Telebond updated to {maxTelebond} (from {Identity.Name(headRevUid, EntityManager)})"), 
                            implant.ImplantedEntity.Value, implant.ImplantedEntity.Value, PopupType.Medium);
                    }
                }
                
                if (store.Balance["Conversion"] < maxConversion)
                {
                    store.Balance["Conversion"] = maxConversion;
                    Logger.Info($"Updated Conversion currency in uplink {ToPrettyString(uplink)} to {maxConversion}");
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
                Logger.Info($"Updated global Conversion currency in uplink {ToPrettyString(metadata.Owner)} to {maxConversion}");
            }
            
            // Check if this uplink has a higher Conversion value
            if (uplinkStore.Balance["Conversion"] > maxConversion)
            {
                maxConversion = uplinkStore.Balance["Conversion"];
                Logger.Info($"Found higher global Conversion value: {maxConversion}");
                
                // Update all uplinks we've already processed with this higher value
                foreach (var processedUplink in allUplinks)
                {
                    if (TryComp<StoreComponent>(processedUplink, out var processedStore))
                    {
                        processedStore.Balance["Conversion"] = maxConversion;
                        Logger.Info($"Updated Conversion currency in uplink {ToPrettyString(processedUplink)} to {maxConversion}");
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
        Logger.Info("Adding Conversion to all head revolutionary uplinks");
        
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
            Logger.Warning("No USSP uplink implants found in the game");
            return;
        }
        
        // Add Conversion to all uplinks
        foreach (var uplinkEntity in uplinkEntities)
        {
            var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } };
            var success = storeSystem.TryAddCurrency(currencyToAdd, uplinkEntity);
            Logger.Info($"Added Conversion to uplink {ToPrettyString(uplinkEntity)}, success: {success}");
        }
        
        // Show popup to all head revolutionaries (private)
        var headRevs = AllEntityQuery<HeadRevolutionaryComponent>();
        while (headRevs.MoveNext(out var headRevUid, out _))
        {
            _popup.PopupEntity(Loc.GetString("+1 Conversion"), headRevUid, headRevUid, PopupType.Medium);
        }
    }
}
