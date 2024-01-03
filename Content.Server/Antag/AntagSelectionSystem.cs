using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles.Jobs;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.Player;
using System.Linq;
using Content.Server.Mind;
using Robust.Shared.Random;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Inventory;
using Content.Server.Storage.EntitySystems;
using Robust.Shared.Audio;
using Robust.Server.GameObjects;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Robust.Shared.Containers;
using Content.Shared.Mobs.Components;
using Content.Server.Station.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Shuttles.Components;
using Content.Shared.Roles;
using Content.Shared.Players;

namespace Content.Server.Antag;

public sealed class AntagSelectionSystem : GameRuleSystem<GameRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;

    /// <summary>
    /// Attempts to start the game rule by checking if there are enough players in lobby and readied.
    /// </summary>
    /// <param name="ev">The roundstart attempt event</param>
    /// <param name="uid">The entity the gamerule you are using is on</param>
    /// <param name="minPlayers">The minimum amount of players needed for you gamerule to start.</param>
    /// <param name="gameRule">The gamerule component.</param>

    public void AttemptStartGameRule(RoundStartAttemptEvent ev, EntityUid uid, int minPlayers, GameRuleComponent gameRule, string gameRuleName)
    {
        if (GameTicker.IsGameRuleAdded(uid, gameRule))
        {
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("antag-selection-attempt-start-insufficient-players",
                    ("failedGameMode", gameRuleName),
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers)));
                ev.Cancel();
            }
            else if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("antag-selection-attempt-start-no-players", ("failedGameMode", gameRuleName)));
                ev.Cancel();
            }
        }
    }

    /// <summary>
    /// Get all players that are eligible for an antag role
    /// </summary>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="includeAllJobs">Should jobs that prohibit antag roles (ie Heads, Sec, Interns) be included</param>
    /// <param name="allowMultipleAntagRoles">Should players that already have an antag role be included</param>
    /// <returns></returns>
    public List<EntityUid> GetEligiblePlayers(string antagPrototype, bool includeAllJobs = false, bool allowMultipleAntagRoles = false)
    {
        var allPlayers = _playerSystem.Sessions.ToList();
        var eligiblePlayers = new List<EntityUid>();

        foreach (var player in allPlayers)
        {
            //Ensure the player has a mind
            if (player.GetMind() is not { } playerMind)
                continue;

            //Ensure the player has an attached entity
            if (!player.AttachedEntity.HasValue)
                continue;

            //Ignore latejoined players, ie those on the arrivals station
            if (HasComp<PendingClockInComponent>(player.AttachedEntity))
                continue;

            //Exclude jobs that cannot be antag, unless explicitly allowed
            if (!includeAllJobs && !_jobs.CanBeAntag(player))
                continue;

            //Test is player already is an antag, to prevent double roles
            //As antags are balanced around themselves, introducing additional antag gear (ie Head Rev with thief equipment) can destabilise that balance
            if (!allowMultipleAntagRoles)
            {
                var ev = new MindIsAntagonistEvent();
                RaiseLocalEvent(playerMind, ref ev);

                if (ev.IsAntagonist)
                    continue;
            }

            //Only allow humanoids
            if (!HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
                continue;

            //Test the player has this antag enabled as a preference
            var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(antagPrototype))
                eligiblePlayers.Add(player.AttachedEntity.Value);
        }

        return eligiblePlayers;
    }

    /// <summary>
    /// Helper method to calculate the number of antags to select based upon the number of players
    /// </summary>
    /// <param name="playerCount">How many players there are on the server</param>
    /// <param name="playersPerAntag">How many players should there be for an additional antag</param>
    /// <param name="maxAntags">Maximum number of antags allowed</param>
    /// <returns></returns>
    public int CalculateAntagNumber(int playerCount, int playersPerAntag, int maxAntags)
    {
        return Math.Clamp(playerCount / playersPerAntag, 1, maxAntags);
    }

    /// <summary>
    /// Helper method to choose antags from a list
    /// </summary>
    /// <param name="eligiblePlayers">List of eligible players</param>
    /// <param name="count">How many to choose</param>
    /// <returns></returns>
    public List<EntityUid> ChooseAntags(List<EntityUid> eligiblePlayers, int count)
    {
        var chosenPlayers = new List<EntityUid>();

        for (int i = 0; i < count; i++)
        {
            chosenPlayers.Add(_random.PickAndTake(eligiblePlayers));
        }

        return chosenPlayers;
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a list of players
    /// </summary>
    /// <param name="chosenPlayers">The players chosen to be antags</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(List<EntityUid> chosenPlayers, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var player in chosenPlayers)
        {
            SendBriefing(player, briefing, briefingColor, briefingSound);
        }
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a player
    /// </summary>
    /// <param name="chosenPlayers">The player chosen to be an antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(EntityUid player, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (!_mindSystem.TryGetMind(player, out var mind, out var mindComponent))
            return;

        if (mindComponent.Session == null)
            return;

        _audioSystem.PlayGlobal(briefingSound, player);
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, briefing, wrappedMessage, default, false, mindComponent.Session.Channel, briefingColor);
    }

    /// <summary>
    /// The function walks through all players, checking their role and preferences to generate a list of players who can become antagonists.
    /// </summary>
    /// <param name="candidates">a list of players to check out</param>
    /// <param name="antagPreferenceId">antagonist's code id</param>
    /// <returns></returns>
    public List<ICommonSession> FindPotentialAntags(in Dictionary<ICommonSession, HumanoidCharacterProfile> candidates, string antagPreferenceId)
    {
        var list = new List<ICommonSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!_jobs.CanBeAntag(player))
                continue;

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<ICommonSession>();

        foreach (var player in list)
        {
            //player preferences to play as this antag
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(antagPreferenceId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Log.Info($"Insufficient preferred antag:{antagPreferenceId}, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    /// <summary>
    /// selects the specified number of players from the list
    /// </summary>
    /// <param name="antagCount">how many players to take</param>
    /// <param name="prefList">a list of players from which to draw</param>
    /// <returns></returns>
    public List<ICommonSession> PickAntag(int antagCount, List<ICommonSession> prefList)
    {
        var results = new List<ICommonSession>(antagCount);
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient ready players to fill up with antags, stopping the selection.");
            return results;
        }

        for (var i = 0; i < antagCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Log.Info("Selected a preferred antag.");
        }
        return results;
    }

    /// <summary>
    /// Will take a group of entities and check if they are all alive or dead
    /// </summary>
    /// <param name="list">The list of the entities</param>
    /// <param name="checkOffStation">Bool for if you want to check if someone is in space and consider them dead. (Won't check when emergency shuttle arrives just in case)</param>
    /// <returns></returns>
    public bool IsGroupDead(List<EntityUid> list, bool checkOffStation)
    {
        var dead = 0;
        foreach (var entity in list)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                else if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    dead++;
                }
            }
            //If they don't have the MobStateComponent they might as well be dead.
            else
            {
                dead++;
            }
        }

        return dead == list.Count || list.Count == 0;
    }

    /// <summary>
    /// Will attempt to spawn an item inside of a persons bag and then pockets.
    /// </summary>
    /// <param name="antag">The entity that you want to spawn an item on</param>
    /// <param name="items">A list of prototype IDs that you want to spawn in the bag.</param>
    public void GiveAntagBagGear(EntityUid antag, List<EntProtoId> items)
    {
        foreach (var item in items)
        {
            GiveAntagBagGear(antag, item);
        }
    }

    /// <summary>
    /// Will attempt to spawn an item inside of a persons bag and then pockets.
    /// </summary>
    /// <param name="antag">The entity that you want to spawn an item on</param>
    /// <param name="item">The prototype ID that you want to spawn in the bag.</param>
    public void GiveAntagBagGear(EntityUid antag, string item)
    {
        var itemToSpawn = Spawn(item, new EntityCoordinates(antag, Vector2.Zero));
        if (!_inventory.TryGetSlotContainer(antag, "back", out var backSlot, out _))
            return;

        var bag = backSlot.ContainedEntity;
        if (bag != null && HasComp<ContainerManagerComponent>(bag) && _storageSystem.CanInsert(bag.Value, itemToSpawn, out _))
        {
            _storageSystem.Insert(bag.Value, itemToSpawn, out _);
        }
        else if (_inventory.TryGetSlotContainer(antag, "jumpsuit", out var jumpsuit, out _) && jumpsuit.ContainedEntity != null)
        {
            if (_inventory.TryGetSlotContainer(antag, "pocket1", out var pocket1Slot, out _))
            {
                if (pocket1Slot.ContainedEntity == null)
                {
                    if (_containerSystem.CanInsert(itemToSpawn, pocket1Slot))
                    {
                        _containerSystem.Insert(itemToSpawn, pocket1Slot);
                    }
                }
                else if (_inventory.TryGetSlotContainer(antag, "pocket2", out var pocket2Slot, out _))
                {
                    if (pocket2Slot.ContainedEntity == null)
                    {
                        if (_containerSystem.CanInsert(itemToSpawn, pocket2Slot))
                        {
                            _containerSystem.Insert(itemToSpawn, pocket2Slot);
                        }
                    }
                }
            }
        }
    }
}

