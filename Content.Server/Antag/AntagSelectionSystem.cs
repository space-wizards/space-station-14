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

    #region Eligible Player Selection
    /// <summary>
    /// Get all players that are eligible for an antag role
    /// </summary>
    /// <param name="playerSessions">All sessions from which to select eligible players</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="includeAllJobs">Should jobs that prohibit antag roles (ie Heads, Sec, Interns) be included</param>
    /// <param name="allowMultipleAntagRoles">Should players that already have an antag role be included</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <param name="customExcludeCondition">A custom condition that each player is tested against, if it returns true the player is excluded from eligibility</param>
    /// <returns>List of all player entities that match the requirements</returns>
    public List<EntityUid> GetEligiblePlayers(ICommonSession[] playerSessions, string antagPrototype, bool includeAllJobs = false, bool allowMultipleAntagRoles = false, bool ignorePreferences = false, Func<EntityUid?, bool>? customExcludeCondition = null)
    {
        var eligiblePlayers = new List<EntityUid>();

        foreach (var player in playerSessions)
        {
            if (IsEligible(player, antagPrototype, includeAllJobs, allowMultipleAntagRoles, ignorePreferences, customExcludeCondition))
                eligiblePlayers.Add(player.AttachedEntity!.Value);
        }

        return eligiblePlayers;
    }

    /// <summary>
    /// Get all sessions that are eligible for an antag role, can be run prior to sessions being attached to an entity
    /// This does not exclude sessions that have already been chosen as antags - that must be handled manually
    /// </summary>
    /// <param name="playerSessions">All sessions from which to select eligible players</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <returns>List of all player sessions that match the requirements</returns>
    public List<ICommonSession> GetEligibleSessions(ICommonSession[] playerSessions, string antagPrototype, bool ignorePreferences = false)
    {
        var eligibleSessions = new List<ICommonSession>();

        foreach (var session in playerSessions)
        {
            if (IsSessionEligible(session, antagPrototype, ignorePreferences))
                eligibleSessions.Add(session);
        }

        return eligibleSessions;
    }

    /// <summary>
    /// Test eligibility of the player for a specific antag role
    /// </summary>
    /// <param name="session">The player session to test</param>
    /// <param name="antagPrototype">The prototype to get eligible players for</param>
    /// <param name="includeAllJobs">Should jobs that prohibit antag roles (ie Heads, Sec, Interns) be included</param>
    /// <param name="allowMultipleAntagRoles">Should players that already have an antag role be included</param>
    /// <param name="ignorePreferences">Should we ignore if the player has enabled this specific role</param>
    /// <param name="customExcludeCondition">A function, accepting an EntityUid and returning bool. Each player is tested against this, returning truw will exclude the player from eligibility</param>
    /// <returns>True if the player session matches the requirements, false otherwise</returns>
    public bool IsEligible(ICommonSession session, string antagPrototype, bool includeAllJobs = false, bool allowMultipleAntagRoles = false, bool ignorePreferences = false, Func<EntityUid?, bool>? customExcludeCondition = null)
    {
        if (!IsSessionEligible(session, antagPrototype, ignorePreferences))
            return false;

        //Ensure the player has a mind
        if (session.GetMind() is not { } playerMind)
            return false;

        //Ensure the player has an attached entity
        if (!session.AttachedEntity.HasValue)
            return false;

        //Ignore latejoined players, ie those on the arrivals station
        if (HasComp<PendingClockInComponent>(session.AttachedEntity))
            return false;

        //Exclude jobs that cannot be antag, unless explicitly allowed
        if (!includeAllJobs && !_jobs.CanBeAntag(session))
            return false;

        //Test is player already is an antag, to prevent double roles
        //As antags are balanced around themselves, introducing additional antag gear (ie Head Rev with thief equipment) can destabilise that balance
        if (!allowMultipleAntagRoles)
        {
            var ev = new MindIsAntagonistEvent();
            RaiseLocalEvent(playerMind, ref ev);

            if (ev.IsAntagonist)
                return false;
        }

        //Only allow humanoids
        if (!HasComp<HumanoidAppearanceComponent>(session.AttachedEntity))
            return false;

        //If a custom condition was provided, test it and exclude the player if it returns true
        if (customExcludeCondition != null && customExcludeCondition(session.AttachedEntity))
            return false;


        return true;
    }

    /// <summary>
    /// Check if the session is eligible for a role, can be run prior to the session being attached to an entity
    /// </summary>
    /// <param name="session">Player session to check</param>
    /// <param name="antagPrototype">Which antag prototype to check for</param>
    /// <param name="ignorePreferences">Ignore if the player has enabled this antag</param>
    /// <returns>True if the session matches the requirements, false otherwise</returns>
    public bool IsSessionEligible(ICommonSession session, string antagPrototype, bool ignorePreferences = false)
    {
        //Exclude disconnected or zombie sessions
        //No point giving antag roles to them
        if (session.Status == Robust.Shared.Enums.SessionStatus.Disconnected)
            return false;

        if (session.Status == Robust.Shared.Enums.SessionStatus.Zombie)
            return false;

        //Check the player has this antag preference selected
        //Unless we are ignoring preferences, in which case add them anyway
        var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(session.UserId).SelectedCharacter;
        if (!pref.AntagPreferences.Contains(antagPrototype) && !ignorePreferences)
            return false;

        return true;
    }
    #endregion

    /// <summary>
    /// Helper method to calculate the number of antags to select based upon the number of players
    /// </summary>
    /// <param name="playerCount">How many players there are on the server</param>
    /// <param name="playersPerAntag">How many players should there be for an additional antag</param>
    /// <param name="maxAntags">Maximum number of antags allowed</param>
    /// <returns>The number of antags that should be chosen</returns>
    public int CalculateAntagCount(int playerCount, int playersPerAntag, int maxAntags)
    {
        return Math.Clamp(playerCount / playersPerAntag, 1, maxAntags);
    }

    #region Antag Selection
    /// <summary>
    /// Helper method to choose antags from a list
    /// </summary>
    /// <param name="eligiblePlayers">List of eligible players</param>
    /// <param name="count">How many to choose</param>
    /// <returns>Up to the specified count of elements from the provided list</returns>
    public List<T> ChooseAntags<T>(List<T> eligiblePlayers, int count)
    {
        var chosenPlayers = new List<T>();

        for (int i = 0; i < count; i++)
        {
            if (eligiblePlayers.Count == 0)
                break;

            chosenPlayers.Add(_random.PickAndTake(eligiblePlayers));
        }

        return chosenPlayers;
    }

    /// <summary>
    /// Selects a set number of players from several lists, prioritising the first list till its empty, then second list etc
    /// </summary>
    /// <typeparam name="T">The type of item you are choosing</typeparam>
    /// <param name="eligiblePlayerLists">Array of lists, which are chosen from in order until the correct number of items are selected</param>
    /// <param name="count">How many items to select</param>
    /// <returns>Up to the specified count of elements from all provided lists</returns>
    public List<T> ChooseAntags<T>(List<T>[] eligiblePlayerLists, int count)
    {
        var chosenPlayers = new List<T>();
        foreach (var playerList in eligiblePlayerLists)
        {
            //Remove all chosen players from this list, to prevent duplicates
            foreach (var chosenPlayer in chosenPlayers)
                playerList.Remove(chosenPlayer);

            //If we have reached the desired number of players, skip
            if (chosenPlayers.Count >= count)
                continue;

            //Pick and choose a random number of players from this list
            chosenPlayers.AddRange(ChooseAntags<T>(playerList, count - chosenPlayers.Count));
        }
        return chosenPlayers;
    }
    #endregion

    #region Briefings
    /// <summary>
    /// Helper method to send the briefing text and sound to a list of entities
    /// </summary>
    /// <param name="entities">The players chosen to be antags</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(List<EntityUid> entities, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var entity in entities)
        {
            SendBriefing(entity, briefing, briefingColor, briefingSound);
        }
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a player entity
    /// </summary>
    /// <param name="entity">The entity chosen to be antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(EntityUid entity, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (!_mindSystem.TryGetMind(entity, out var mind, out var mindComponent))
            return;

        if (mindComponent.Session == null)
            return;

        SendBriefing(mindComponent.Session, briefing, briefingColor, briefingSound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a list of sessions
    /// </summary>
    /// <param name="sessions"></param>
    /// <param name="briefing"></param>
    /// <param name="briefingColor"></param>
    /// <param name="briefingSound"></param>

    public void SendBriefing(List<ICommonSession> sessions, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var session in sessions)
        {
            SendBriefing(session, briefing, briefingColor, briefingSound);
        }
    }
    /// <summary>
    /// Helper method to send the briefing text and sound to a session
    /// </summary>
    /// <param name="session">The player chosen to be an antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>

    public void SendBriefing(ICommonSession session, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        _audioSystem.PlayGlobal(briefingSound, session);
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel, briefingColor);
    }
    #endregion

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

