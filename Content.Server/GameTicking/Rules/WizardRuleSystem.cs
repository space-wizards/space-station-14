
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Administration.Commands;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Roles;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Antag;
using Content.Shared.Store;
using Content.Server.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Station.Components;
using Content.Server.NPC.Components;
using Content.Shared.Preferences;
using Content.Server.NPC.Systems;
using Content.Server.Spawners.Components;
using Content.Shared.Preferences;
using Content.Server.Weapons.Melee.WeaponRandom;
using JetBrains.Annotations;
using System.Reflection.Metadata.Ecma335;
using Content.Shared.NPC.Systems;
using Content.Server.Preferences.Managers;
using Robust.Server.GameObjects;
using Prometheus;
using Robust.Server.Maps;
using System.Numerics;
using Content.Shared.Mind;
using Content.Server.Objectives;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Mind.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class WizardRuleSystem : GameRuleSystem<WizardRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly InventorySystem _inventoryManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private ISawmill _sawmill = default!;






    [ValidatePrototypeId<AntagPrototype>]
    private const string WizardId = "Wizard";

    [ValidatePrototypeId<CurrencyPrototype>]
    private const string MagipointsCurrencyPrototype = "Magipoint";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);

    }

    protected override void Started(EntityUid uid, WizardRuleComponent component, GameRuleComponent gameRule,
      GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        //if (GameTicker.RunLevel == GameRunLevel.InRound)
        // SpawnOperativesForGhostRoles(uid, component);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        ev.AddLine(Loc.GetString("wizard-list-start"));

        var wizQuery = EntityQueryEnumerator<WizardRoleComponent, MindContainerComponent>();
        while (wizQuery.MoveNext(out var wizUid, out _, out var mindContainer))
        {
            if (!_mind.TryGetMind(wizUid, out _, out var mind, mindContainer))
                continue;

            ev.AddLine(mind.Session != null
               ? Loc.GetString("wizard-list-name-user", ("name", Name(wizUid)), ("user", mind.Session.Name))
               : Loc.GetString("wizard-list-name", ("name", Name(wizUid))));
        }
    }


    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, "placeholder");
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var wizards, out _))
        {

            SpawnMap(wizards);

            //Handle there being nobody readied up
            if (ev.PlayerPool.Count == 0)
                continue;

            var wizardEligible = _antagSelection.GetEligibleSessions(ev.PlayerPool, wizards.WizardPrototypeId);
            // 2 wiznerds
            var wizardSelectionCandidates = _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, 20, 1);

            //Select Wiznerds
            var selectedWizard = _antagSelection.ChooseAntags(2, wizardEligible, ev.PlayerPool).FirstOrDefault();


            // var wizkids = new List<ICommonSession> { selectedWizard };
            var wizkids = new List<WizardSpawn> { new WizardSpawn(selectedWizard) };
            if (wizardSelectionCandidates > 1)
                wizkids.Add(new WizardSpawn(selectedWizard));



            SpawnWizards(wizkids, true, wizards);

            foreach (var wizSpawn in wizkids)
            {
                if (wizSpawn.Session == null)
                    continue;

                GameTicker.PlayerJoinGame(wizSpawn.Session);
            }
        }
    }

    private void SpawnMap(WizardRuleComponent component)
    {

        var map = "/Maps/Shuttles/wizardimproved.yml";
        var xformQuery = GetEntityQuery<TransformComponent>();

        var aabbs = EntityQuery<StationDataComponent>().SelectMany(x =>
                x.Grids.Select(x =>
                    xformQuery.GetComponent(x).WorldMatrix.TransformBox(Comp<MapGridComponent>(x).LocalAABB)))
            .ToArray();

        var aabb = aabbs[0];

        for (var i = 1; i < aabbs.Length; i++)
        {
            aabb.Union(aabbs[i]);
        }

        // (Not commented?)
        var a = MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * 2.5f;

        var gridId = _map.LoadGrid(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = aabb.Center + new Vector2(a, a),
            LoadMap = false,
        });

        if (!gridId.HasValue)
            return;

        component.WizardShuttle = gridId.Value;
    }

    public void SetupWizard(EntityUid mob, string name, HumanoidCharacterProfile? profile, WizardRuleComponent component)
    {
        _metaData.SetEntityName(mob, name);
        EnsureComp<WizardRoleComponent>(mob);

        if (profile != null)
            _humanoid.LoadProfile(mob, profile);

        var gear = _prototypeManager.Index(component.GearProto);
        _stationSpawning.EquipStartingGear(mob, gear);
        _inventoryManager.SpawnItemOnEntity(mob, "BaseSpellbookShop10MP");

        _npcFaction.RemoveFaction(mob, "NanoTrasen", false);
        _npcFaction.AddFaction(mob, "WizardFederation");
        _npcFaction.MakeHostile("WizardFederation", "NanoTrasen");

    }

    private void SpawnWizards(List<WizardSpawn> sessions, bool spawnGhostRoles, WizardRuleComponent component)
    {


        var spawns = new List<EntityCoordinates>();
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != component.SpawnPointProto.Id)
                continue;

            if (xform.ParentUid != component.WizardShuttle)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        //Fallback, spawn at the centre of the map
        if (spawns.Count == 0)
        {
            spawns.Add(Transform(component.WizardShuttle).Coordinates);
            _sawmill.Warning($"Fell back to default spawn for nukies!");
        }

        //Spawn the team
        foreach (var wizSessions in sessions)
        {





            var wizardAntag = component.WizardPrototypeId;

            //If a session is available, spawn mob and transfer mind into it
            if (wizSessions.Session != null)
            {
                var profile = _prefs.GetPreferences(wizSessions.Session.UserId).SelectedCharacter as HumanoidCharacterProfile;
                if (!_prototypeManager.TryIndex(profile?.Species ?? SharedHumanoidAppearanceSystem.DefaultSpecies, out SpeciesPrototype? species))
                {
                    species = _prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
                }

                var mob = Spawn(species.Prototype, RobustRandom.Pick(spawns));
                var name = "Wizkid";
                if (TryComp<HumanoidAppearanceComponent>(mob, out var humanoid))
                {
                    var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
                    _humanoid.LoadProfile(mob, newProfile, humanoid);
                    _metaData.SetEntityName(mob, newProfile.Name);
                    name = newProfile.Name;
                }


                SetupWizard(mob, name, profile, component);

                var newMind = _mind.CreateMind(wizSessions.Session.UserId, name);
                _mind.SetUserId(newMind, wizSessions.Session.UserId);
                _roles.MindAddRole(newMind, new WizardRoleComponent { PrototypeId = component.WizardPrototypeId });

                // Automatically de-admin players who are being made nukeops
                if (_cfg.GetCVar(CCVars.AdminDeadminOnJoin) && _adminManager.IsAdmin(wizSessions.Session))
                    _adminManager.DeAdmin(wizSessions.Session);

                var onlyObjective = _objectives.TryCreateObjective(mob, newMind, "WizardOnlyObjective");
                if (onlyObjective != null)
                    _mind.AddObjective(mob, newMind, onlyObjective.Value);

                _mind.TransferTo(newMind, mob);
                _antagSelection.SendBriefing(mob, MakeBriefing(mob), null, component.GreetingSound);
            }
            //Otherwise, spawn as a ghost role
            /* else if (spawnGhostRoles)
             {
                 var spawnPoint = Spawn(component.SpawnPointProto, RobustRandom.Pick(spawns));
                 var ghostRole = EnsureComp<GhostRoleComponent>(spawnPoint);
                 EnsureComp<GhostRoleMobSpawnerComponent>(spawnPoint);
                 ghostRole.RoleName = Loc.GetString(name);
                 ghostRole.RoleDescription = "wip";
             }*/
        }
    }

    public void RemoveItems(EntityUid player)
    {
        // checks for pre-existing items and deletes them
        EntityManager.TryGetComponent(player, out InventoryComponent? inventoryComponent);
        if (_inventoryManager.TryGetSlots(player, out var slots))
        {
            foreach (var slot in slots)
            {
                if (_inventoryManager.TryGetSlotEntity(player, slot.Name, out var itemUid, inventoryComponent))
                {
                    _entityManager.DeleteEntity(itemUid);
                }
            }
        }
    }

    public void SpawnItems(EntityUid player)
    {
        // holy shit this is so jank
        // TODO - Make this system run through a gear prototype thing
        EntityManager.TryGetComponent(player, out InventoryComponent? inventoryComponent);
        RemoveItems(player);
        _inventoryManager.SpawnItemInSlot(player, "shoes", "ClothingShoesWizard", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "jumpsuit", "ClothingUniformJumpsuitColorDarkBlue", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "back", "ClothingBackpackFilled", true, false, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "head", "ClothingHeadHatWizard", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "outerClothing", "ClothingOuterWizard", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "id", "WizardPDA", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "ears", "ClothingHeadsetService", true, true, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "innerClothingSkirt", "ClothingUniformJumpskirtColorDarkBlue", true, false, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "satchel", "ClothingBackpackSatchelFilled", true, false, inventoryComponent);
        _inventoryManager.SpawnItemInSlot(player, "duffelbag", "ClothingBackpackDuffelFilled", true, false, inventoryComponent);
    }

    public void AdminMakeWizard(EntityUid entity)
    {
        // Admin Verb for wizard-ification
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        SpawnItems(entity);
        _inventoryManager.SpawnItemOnEntity(entity, "BaseSpellbookShop10MP");
        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "Wizard");
        EnsureComp<WizardRoleComponent>(entity);

        var wizardRule = EntityQuery<WizardRuleComponent>().FirstOrDefault();




        if (wizardRule == null)
        {
            // Adds wizard gamerule if not already existing
            GameTicker.StartGameRule("Wizard", out var ruleEntity);
            wizardRule = Comp<WizardRuleComponent>(ruleEntity);
            SpawnMap(wizardRule);
        }


        var onlyObjective = _objectives.TryCreateObjective(entity, mind, "WizardOnlyObjective");
        if (onlyObjective != null)
            _mind.AddObjective(entity, mind, onlyObjective.Value);

        _antagSelection.SendBriefing(entity, MakeBriefing(entity), null, wizardRule.GreetingSound);


    }


    private string MakeBriefing(EntityUid wizard)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(wizard);
        var briefing = "\n";
        briefing = isHuman
            ? Loc.GetString("wizard-welcome")
            : Loc.GetString("wizard-welcome");

        briefing += "\n \n" + Loc.GetString("wizard-description") + "\n";
        return briefing;
    }

    private sealed class WizardSpawn
    {
        public ICommonSession? Session { get; private set; }

        public WizardSpawn(ICommonSession? session)
        {
            Session = session;
        }
    }


}

