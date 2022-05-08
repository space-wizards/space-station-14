using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

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
