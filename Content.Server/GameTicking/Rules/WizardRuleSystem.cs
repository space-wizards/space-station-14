
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
using Content.Server.Weapons.Melee.WeaponRandom;
using JetBrains.Annotations;

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
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly InventorySystem _inventoryManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private ISawmill _sawmill = default!;


    [ValidatePrototypeId<AntagPrototype>]
    private const string WizardId = "Wizard";

    [ValidatePrototypeId<CurrencyPrototype>]
    private const string MagipointsCurrencyPrototype = "Magipoint";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("Wizards");
        // SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        // SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        //  SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        //  SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);

    }


    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {

            var eligiblePlayers = _antagSelection.GetEligiblePlayers(ev.Players, comp.WizardPrototypeId, acceptableAntags: AntagAcceptability.All, allowNonHumanoids: true);


            if (eligiblePlayers.Count == 0)
            {
                Log.Warning($"No eligible thieves found, ending game rule {ToPrettyString(uid):rule}");
                GameTicker.EndGameRule(uid, gameRule);
                continue;
            }

            //Calculate number of thieves to choose
            var wizardCount = _random.Next(1, comp.MaxWizards + 1);

            //Select our theives
            var wizards = _antagSelection.ChooseAntags(wizardCount, eligiblePlayers);

            MakeWizard(wizards, comp);
        }
    }

    public void MakeWizard(List<EntityUid> players, WizardRuleComponent wizardRule)
    {
        foreach (var wizard in players)
        {
            MakeWizard(wizard, wizardRule);
        }
    }

    public void MakeWizard(EntityUid player, WizardRuleComponent wizardRule)
    {
        if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
            return;

        if (HasComp<WizardRoleComponent>(mindId))
            return;

        _roles.MindAddRole(mindId, new WizardRoleComponent
        {
            PrototypeId = wizardRule.WizardPrototypeId,
        }, silent: true);

        SpawnItems(player);
        wizardRule.Wizards.Add(mindId);

    }

    public void SpawnItems(EntityUid player)
    {
        // holy shit this is so jank
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

    public void RemoveItems(EntityUid player)
    {
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

    public void AdminMakeWizard(EntityUid entity)
    {
        if (!_mindSystem.TryGetMind(entity, out var mindId, out var mind))
            return;

        SpawnItems(entity);

        var wizardRule = EntityQuery<WizardRuleComponent>().FirstOrDefault();
        if (wizardRule == null)
        {

            GameTicker.StartGameRule("Wizard", out var ruleEntity);
            wizardRule = Comp<WizardRuleComponent>(ruleEntity);
        }



    }
}
