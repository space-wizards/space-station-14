using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Nukeops;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly AccessSystem _accessSystem = default!;


    public override string Prototype => "Nukeops";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        //SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        var numOps = (int)Math.Min(Math.Floor((double)ev.PlayerPool.Count / _cfg.GetCVar(CCVars.NukeopsPlayersPerOp)),
            _cfg.GetCVar(CCVars.NukeopsMaxOps));
        var ops = new IPlayerSession[numOps];
        for (var i = 0; i < numOps; i++)
        {
            ops[i] = _random.PickAndTake(ev.PlayerPool);
        }

        //todo make configurable
        var map = "/Maps/knightshuttle.yml";
        var (_, grid) = _mapLoader.LoadBlueprint(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = new Vector2(-500, -500)
        });

        if (!grid.HasValue)
        {
            Logger.ErrorS("NUKEOPS", $"Gridid was null when loading \"{map}\", aborting.");
            foreach (var session in ops)
            {
                ev.PlayerPool.Add(session);
            }
            return;
        }

        //todo spawners
        var gear = _prototypeManager.Index<StartingGearPrototype>("NukeopsGear");
        var accessLevel = _prototypeManager.Index<AccessLevelPrototype>("NuclearOperative");
        var roles = _prototypeManager.EnumeratePrototypes<NukeopsRolePrototype>().ToList();
        var spawnpos = new EntityCoordinates(_mapManager.GetGridEuid(grid.Value), Vector2.Zero);
        for (var i = 0; i < ops.Length; i++)
        {
            var session = ops[i];
            var name = $"Operator #{i}";
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);

            var mob = EntityManager.SpawnEntity("MobHuman", spawnpos);
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;

            newMind.TransferTo(mob);
            GameTicker.EquipStartingGear(mob, gear, null);

            //todo make this a preference
            var role = _random.Pick(roles);

            var duffel = EntityManager.SpawnEntity(role.Back, spawnpos);
            _inventorySystem.TryEquip(mob, duffel, "back", true, true);

            if (_inventorySystem.TryGetSlotEntity(mob, "id", out var idUid))
            {
                _accessSystem.TrySetTags(idUid.Value, new[] { accessLevel.ID });
            }

            GameTicker.PlayerJoinGame(session);
        }
    }

    // todo paul do loc
    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!Enabled)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.NukeopsMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
            ev.Cancel();
            return;
        }
    }


    public override void Started()
    {
        throw new NotImplementedException();
    }

    public override void Ended()
    {
        throw new NotImplementedException();
    }
}
