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
using Robust.Server.Containers;
using Robust.Shared.Prototypes;

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

    public void AttemptStartGameRule(RoundStartAttemptEvent ev, EntityUid uid, int minPlayers, GameRuleComponent gameRule)
    {
        if (GameTicker.IsGameRuleAdded(uid, gameRule))
        {
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("rev-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers)));
                ev.Cancel();
            }
            else if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rev-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    /// <summary>
    /// Will check which players are eligible to be chosen for antagonist and give them the given antag.
    /// </summary>
    /// <param name="antagPrototype">The antag prototype from your rule component.</param>
    /// <param name="maxAntags">How many antags can be present in any given round.</param>
    /// <param name="antagsPerPlayer">How many players you need to spawn an additional antag.</param>
    /// <param name="antagSound">The intro sound that plays when the antag is chosen.</param>
    /// <param name="antagGreeting">The antag message you want shown when the antag is chosen.</param>
    /// <param name="greetingColor">The color of the message for the antag greeting in hex.</param>
    /// <param name="chosen">A list of all the antags chosen in case you need to add stuff after.</param>
    /// <param name="includeHeads">Whether or not heads can be chosen as antags for this gamemode.</param>
    public void EligiblePlayers(string antagPrototype,
        int maxAntags,
        int antagsPerPlayer,
        SoundSpecifier? antagSound,
        string antagGreeting,
        string greetingColor,
        out List<EntityUid> chosen,
        bool includeHeads = false)
    {
        var allPlayers = _playerSystem.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();
        chosen = new List<EntityUid>();
        foreach (var player in allPlayers)
        {
            if (includeHeads == false)
            {
                if (!_jobs.CanBeAntag(player))
                    continue;
            }

            if (player.AttachedEntity == null || HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
                playerList.Add(player);
            else
                continue;

            var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(antagPrototype))
                prefList.Add(player);
        }

        if (playerList.Count == 0)
            return;

        var antags = Math.Clamp(allPlayers.Count / antagsPerPlayer, 1, maxAntags);
        for (var antag = 0; antag < antags; antag++)
        {
            IPlayerSession chosenPlayer;
            if (prefList.Count == 0)
            {
                if (playerList.Count == 0)
                {
                    break;
                }
                chosenPlayer = _random.PickAndTake(playerList);
            }
            else
            {
                chosenPlayer = _random.PickAndTake(prefList);
                playerList.Remove(chosenPlayer);
            }

            if (!_mindSystem.TryGetMind(chosenPlayer, out _, out var mind) ||
               mind.OwnedEntity is not { } ownedEntity)
            {
                continue;
            }

            chosen.Add(ownedEntity);
            _audioSystem.PlayGlobal(antagSound, ownedEntity);
            if (mind.Session != null)
            {
                var message = Loc.GetString(antagGreeting);
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.FromHex(greetingColor));
            }
        }
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
                        pocket1Slot.Insert(itemToSpawn);
                    }
                }
                else if (_inventory.TryGetSlotContainer(antag, "pocket2", out var pocket2Slot, out _))
                {
                    if (pocket2Slot.ContainedEntity == null)
                    {
                        if (_containerSystem.CanInsert(itemToSpawn, pocket2Slot))
                        {
                            pocket2Slot.Insert(itemToSpawn);
                        }
                    }
                }
            }
        }
    }
}

