using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Flesh;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Components;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Radio.Components;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.Flesh;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class FleshCultRuleSystem : GameRuleSystem<FleshCultRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly FactionSystem _faction = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private ISawmill _sawmill = default!;

    private int PlayersPerCultist => _cfg.GetCVar(CCVars.FleshCultPlayersPerCultist);
    private int MaxCultists => _cfg.GetCVar(CCVars.FleshCultMaxCultist);

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<FleshHeartSystem.FleshHeartFinalEvent>(OnFleshHeartFinal);
    }

    private void OnFleshHeartFinal(FleshHeartSystem.FleshHeartFinalEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        Logger.Info("Get FleshHeartFinalEvent");
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
            {
                Logger.Info("FleshCultRule not added");
                continue;
            }

            if (ev.OwningStation == null)
            {
                Logger.Info("OwningStation is null");
                return;
            }

            if (fleshCult.TargetStation == null)
            {
                Logger.Info("TargetStation is null");
                return;
            }

            Logger.Info(fleshCult.TargetStation.Value.ToString());

            if (!TryComp(fleshCult.TargetStation, out StationDataComponent? data))
            {
                Logger.Info("TargetStation not have StationDataComponent");
                return;
            }
            foreach (var grid in data.Grids)
            {
                if (grid != ev.OwningStation)
                {
                    Logger.Info("grid not be TargetStation");
                    continue;
                }

                Logger.Info("FleshHeart Win");
                fleshCult.WinType = FleshCultRuleComponent.WinTypes.FleshHeartFinal;
                _roundEndSystem.EndRound();
                return;
            }
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.FleshCultMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("flesh-cult-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("flesh-cult-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void DoCultistStart(FleshCultRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            _sawmill.Error("Tried to start FleshCult mode without any candidates.");
            return;
        }

        component.TargetStation = _stationSystem.GetStations().FirstOrNull();

        if (component.TargetStation == null)
        {
            _sawmill.Error("No found target station for flesh cult.");
            return;
        }

        var numCultists = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerCultist, 1, MaxCultists);

        IPlayerSession cultistsLeader = default!;
        var cultistsLeaderPool = FindPotentialCultistsLeader(component.StartCandidates, component);
        if (cultistsLeaderPool.Count != 0)
        {
            cultistsLeader = _random.PickAndTake(cultistsLeaderPool);
            numCultists += -1;
        }

        var cultistsPool = FindPotentialCultists(component.StartCandidates, component);
        var selectedCultists = PickCultists(numCultists, cultistsPool);
        selectedCultists.Remove(cultistsLeader);

        foreach (var selectedCultist in selectedCultists)
        {
            var mind = selectedCultist.Data.ContentData()?.Mind;
            var name = mind?.CharacterName;
            if (name != null)
                component.CultistsNames.Add(name);
        }

        if (cultistsLeader != default!)
        {
            MakeCultistLeader(cultistsLeader);
            var mind = cultistsLeader.Data.ContentData()?.Mind;
            var name = mind?.CharacterName;
            if (name != null)
                component.CultistsNames.Add(name);
        }

        foreach (var cultist in selectedCultists)
            MakeCultist(cultist);

        component.SelectionStatus = FleshCultRuleComponent.SelectionState.SelectionMade;
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {

            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                fleshCult.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            DoCultistStart(fleshCult);

            fleshCult.SelectionStatus = FleshCultRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    public List<IPlayerSession> FindPotentialCultists(in Dictionary<IPlayerSession,
        HumanoidCharacterProfile> candidates, FleshCultRuleComponent component)
    {
        var list = new List<IPlayerSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            if (player.Data.ContentData()?.Mind?.AllRoles.Count() > 1)
            {
                continue;
            }

            // Role prevents antag.
            if (!(player.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false))
            {
                continue;
            }

            if (TryComp<HumanoidAppearanceComponent>(player.Data.ContentData()?.Mind?.OwnedEntity, out var appearanceComponent))
            {
                if (!component.SpeciesWhitelist.Contains(appearanceComponent.Species))
                    continue;
            }

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.FleshCultistPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient preferred traitors, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public List<IPlayerSession> FindPotentialCultistsLeader(in Dictionary<IPlayerSession,
        HumanoidCharacterProfile> candidates, FleshCultRuleComponent component)
    {
        var list = new List<IPlayerSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!(player.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false))
            {
                continue;
            }

            if (TryComp<HumanoidAppearanceComponent>(player.Data.ContentData()?.Mind?.OwnedEntity, out var appearanceComponent))
            {
                if (!component.SpeciesWhitelist.Contains(appearanceComponent.Species))
                    continue;
            }

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.FleshCultistLeaderPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient preferred traitors, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public List<IPlayerSession> PickCultists(int cultistCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(cultistCount);
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient ready players to fill up with traitors, stopping the selection.");
            return results;
        }

        for (var i = 0; i < cultistCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            _sawmill.Info("Selected a preferred traitor.");
        }
        return results;
    }

    public bool MakeCultist(IPlayerSession traitor)
    {
        var fleshCultRule = EntityQuery<FleshCultRuleComponent>().FirstOrDefault();
        if (fleshCultRule == null)
        {
            //todo fuck me this shit is awful
            GameTicker.StartGameRule("FleshCult", out var ruleEntity);
            fleshCultRule = EntityManager.GetComponent<FleshCultRuleComponent>(ruleEntity);
        }

        var mind = traitor.Data.ContentData()?.Mind;
        if (mind == null)
        {
            _sawmill.Info("Failed getting mind for picked cultist.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Logger.ErrorS("preset", "Mind picked for traitor did not have an attached entity.");
            return false;
        }

        DebugTools.AssertNotNull(mind.OwnedEntity);

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(fleshCultRule.FleshCultistPrototypeId);
        var cultistRole = new FleshCultistRole(mind, antagPrototype);
        mind.AddRole(cultistRole);
        fleshCultRule.Cultists.Add(cultistRole);
        cultistRole.GreetCultist(fleshCultRule.CultistsNames);

        _faction.RemoveFaction(entity, "NanoTrasen", false);
        _faction.AddFaction(entity, "Flesh");

        var storeComp = EnsureComp<StoreComponent>(mind.OwnedEntity.Value);

        EnsureComp<IntrinsicRadioReceiverComponent>(mind.OwnedEntity.Value);
        var radio = EnsureComp<ActiveRadioComponent>(mind.OwnedEntity.Value);
        radio.Channels.Add("Flesh");
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(mind.OwnedEntity.Value);
        transmitter.Channels.Add("Flesh");

        storeComp.Categories.Add("FleshCultistAbilities");
        storeComp.CurrencyWhitelist.Add("StolenMutationPoint");
        storeComp.BuySuccessSound = fleshCultRule.BuySuccesSound;

        EnsureComp<FleshCultistComponent>(mind.OwnedEntity.Value);

        if (_prototypeManager.TryIndex<ObjectivePrototype>("CreateFleshHeartObjective", out var fleshHeartObjective))
        {
            cultistRole.Mind.TryAddObjective(fleshHeartObjective);
        }
        if (_prototypeManager.TryIndex<ObjectivePrototype>("FleshCultistSurvivalObjective", out var fleshCultistObjective))
        {
            cultistRole.Mind.TryAddObjective(fleshCultistObjective);
        }

        cultistRole.Mind.Briefing = Loc.GetString("flesh-cult-role-cult-members",
            ("cultMembers", string.Join(", ", fleshCultRule.CultistsNames)));

        _audioSystem.PlayGlobal(fleshCultRule.AddedSound, Filter.Empty().AddPlayer(traitor), false, AudioParams.Default);
        return true;
    }

    public bool MakeCultistLeader(IPlayerSession traitor)
    {
        var fleshCultRule = EntityQuery<FleshCultRuleComponent>().FirstOrDefault();
        if (fleshCultRule == null)
        {
            //todo fuck me this shit is awful
            GameTicker.StartGameRule("FleshCult", out var ruleEntity);
            fleshCultRule = EntityManager.GetComponent<FleshCultRuleComponent>(ruleEntity);
        }

        var mind = traitor.Data.ContentData()?.Mind;
        if (mind == null)
        {
            _sawmill.Info("Failed getting mind for picked cultist.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Logger.ErrorS("preset", "Mind picked for traitor did not have an attached entity.");
            return false;
        }

        DebugTools.AssertNotNull(mind.OwnedEntity);

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(fleshCultRule.FleshCultistLeaderPrototypeId);
        var cultistRole = new FleshCultistRole(mind, antagPrototype);
        mind.AddRole(cultistRole);
        fleshCultRule.Cultists.Add(cultistRole);
        cultistRole.GreetCultistLeader(fleshCultRule.CultistsNames);

        _faction.RemoveFaction(entity, "NanoTrasen", false);
        _faction.AddFaction(entity, "Flesh");

        var storeComp = EnsureComp<StoreComponent>(mind.OwnedEntity.Value);

        EnsureComp<IntrinsicRadioReceiverComponent>(mind.OwnedEntity.Value);
        var radio = EnsureComp<ActiveRadioComponent>(mind.OwnedEntity.Value);
        radio.Channels.Add("Flesh");
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(mind.OwnedEntity.Value);
        transmitter.Channels.Add("Flesh");

        storeComp.Categories.Add("FleshCultistAbilities");
        storeComp.CurrencyWhitelist.Add("StolenMutationPoint");
        storeComp.BuySuccessSound = fleshCultRule.BuySuccesSound;

        var fleshCultistComponent = EnsureComp<FleshCultistComponent>(mind.OwnedEntity.Value);

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {fleshCultistComponent.StolenCurrencyPrototype, 50} }, mind.OwnedEntity.Value);

        if (_prototypeManager.TryIndex<ObjectivePrototype>("CreateFleshHeartObjective", out var fleshHeartObjective))
        {
            cultistRole.Mind.TryAddObjective(fleshHeartObjective);
        }
        if (_prototypeManager.TryIndex<ObjectivePrototype>("FleshCultistSurvivalObjective", out var fleshCultistObjective))
        {
            cultistRole.Mind.TryAddObjective(fleshCultistObjective);
        }

        cultistRole.Mind.Briefing = Loc.GetString("flesh-cult-role-cult-members",
            ("cultMembers", string.Join(", ", fleshCultRule.CultistsNames)));

        _audioSystem.PlayGlobal(fleshCultRule.AddedSound, Filter.Empty().AddPlayer(traitor), false, AudioParams.Default);
        return true;
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            if (fleshCult.TotalCultists >= MaxCultists)
                return;
            if (!ev.LateJoin)
                return;
            if (!ev.Profile.AntagPreferences.Contains(fleshCult.FleshCultistPrototypeId))
                return;


            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                return;

            if (!job.CanBeAntag)
                return;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (fleshCult.SelectionStatus < FleshCultRuleComponent.SelectionState.SelectionMade)
            {
                fleshCult.StartCandidates[ev.Player] = ev.Profile;
                return;
            }

            // the nth player we adjust our probabilities around
            int target = ((PlayersPerCultist * fleshCult.TotalCultists) + 1);

            float chance = (1f / PlayersPerCultist);

            // If we have too many traitors, divide by how many players below target for next traitor we are.
            if (ev.JoinOrder < target)
            {
                chance /= (target - ev.JoinOrder);
            }
            else // Tick up towards 100% chance.
            {
                chance *= ((ev.JoinOrder + 1) - target);
            }

            if (chance > 1)
                chance = 1;

            // Now that we've calculated our chance, roll and make them a traitor if we roll under.
            // You get one shot.
            if (_random.Prob(chance))
            {
                MakeCultist(ev.Player);
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var result = Loc.GetString("flesh-cult-round-end-result", ("cultistsCount",
                fleshCult.Cultists.Count));

            if (fleshCult.WinType is FleshCultRuleComponent.WinTypes.FleshHeartFinal)
            {
                result += "\n" + Loc.GetString("flesh-cult-round-end-flesh-heart-succes");
            }
            else
            {
                result += "\n" + Loc.GetString("flesh-cult-round-end-flesh-heart-fail");
            }

            // result += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", Codewords))) + "\n";

            foreach (var cultist in fleshCult.Cultists)
            {
                var name = cultist.Mind.CharacterName;
                cultist.Mind.TryGetSession(out var session);
                var username = session?.Name;

                var objectives = cultist.Mind.AllObjectives.ToArray();

                var leader = "";
                if (cultist.Prototype.ID == fleshCult.FleshCultistLeaderPrototypeId)
                {
                    leader = "-leader";
                }

                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString($"flesh-cult-user-was-a-cultist{leader}",
                                ("user", username));
                        else
                            result += "\n" + Loc.GetString($"flesh-cult-user-was-a-cultist{leader}-named",
                                ("user", username), ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString($"flesh-cult-was-a-cultist{leader}-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString($"flesh-cult-user-was-a-cultist{leader}-with-objectives",
                            ("user", username));
                    else
                        result += "\n" + Loc.GetString($"flesh-cult-user-was-a-cultist{leader}-with-objectives-named",
                            ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString($"flesh-cult-was-a-cultist{leader}-with-objectives-named",
                        ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"preset-flesh-cult-objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "flesh-cult-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "flesh-cult-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }
            result += "\n" +
                      "\n";

            ev.AddLine(result);
        }
    }
}
