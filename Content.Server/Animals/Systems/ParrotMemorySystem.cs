using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Animals.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Radio;
using Content.Server.Vocalization.Systems;
using Content.Shared.Animals.Components;
using Content.Shared.Animals.Systems;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Animals.Systems;

/// <summary>
/// The ParrotMemorySystem handles remembering messages received through local chat (activelistener) or a radio
/// (radiovocalizer) and stores them in a list. When an entity with a VocalizerComponent attempts to vocalize, this will
/// try to set the message from memory.
/// </summary>
public sealed partial class ParrotMemorySystem : SharedParrotMemorySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EraseEvent>(OnErase);

        SubscribeLocalEvent<ParrotListenerComponent, MapInitEvent>(ListenerOnMapInit);
        SubscribeLocalEvent<ParrotMemoryComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<ParrotListenerComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotListenerComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetReceive);

        SubscribeLocalEvent<ParrotMemoryComponent, TryVocalizeEvent>(OnTryVocalize);

    }
    private void OnComponentInit(Entity<ParrotMemoryComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextLearnInterval = _gameTiming.CurTime + ent.Comp.LearnCooldown;

    }
    private void OnErase(ref EraseEvent args)
    {
        DeletePlayerMessages(args.PlayerNetUserId);
    }

    private void ListenerOnMapInit(Entity<ParrotListenerComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotListenerComponent it really ought to have an ActiveListenerComponent
        if (!HasComp<ActiveListenerComponent>(entity))
            Log.Warning($"Entity {ToPrettyString(entity)} has a ParrotListenerComponent but was not given an ActiveListenerComponent");
    }

    private void OnListen(Entity<ParrotListenerComponent> entity, ref ListenEvent args)
    {

        TryLearn(entity.Owner, args.Message, args.Source);
    }

    private void OnHeadsetReceive(Entity<ParrotListenerComponent> entity, ref HeadsetRadioReceiveRelayEvent args)
    {
        var message = args.RelayedEvent.Message;
        var source = args.RelayedEvent.MessageSource;

        TryLearn(entity.Owner, message, source);
    }

    /// <summary>
    /// Called when an entity with a ParrotMemoryComponent tries to vocalize.
    /// This function picks a message from memory and sets the event to handled
    /// </summary>
    private void OnTryVocalize(Entity<ParrotMemoryComponent> entity, ref TryVocalizeEvent args)
    {
        // return if this was already handled
        if (args.Handled)
            return;

        // if there are no memories, return
        if (entity.Comp.SpeechMemories.Count == 0)
            return;

        // get a random memory from the memory list
        var memory = _random.Pick(entity.Comp.SpeechMemories);

        args.Message = memory.Message;
        args.Handled = true;
    }

    /// <summary>
    /// Try to learn a new message, returning early if this entity cannot learn a new message,
    /// the message doesn't pass certain checks, or the chance for learning a new message fails
    /// </summary>
    /// <param name="entity">Entity learning a new word</param>
    /// <param name="incomingMessage">Message to learn</param>
    /// <param name="source">Source EntityUid of the message</param>
    public void TryLearn(Entity<ParrotMemoryComponent?, ParrotListenerComponent?> entity, string incomingMessage, EntityUid source)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return;

        if (!_whitelist.CheckBoth(source, entity.Comp2.Blacklist, entity.Comp2.Whitelist))
            return;

        if (source.Equals(entity) || _mobState.IsIncapacitated(entity))
            return;

        // can't learn too soon after having already learnt something else
        if (_gameTiming.CurTime < entity.Comp1.NextLearnInterval)
            return;

        // remove whitespace around message, if any
        var message = incomingMessage.Trim();

        // ignore messages containing tildes. This is a crude way to ignore whispers that are too far away
        // TODO: this isn't great. This should be replaced with a const or we should have a better way to check faraway messages
        if (message.Contains('~'))
            return;

        // ignore empty messages. These probably aren't sent anyway but just in case
        if (string.IsNullOrWhiteSpace(message))
            return;

        // ignore messages that are too short or too long
        if (message.Length < entity.Comp1.MinEntryLength || message.Length > entity.Comp1.MaxEntryLength)
            return;

        // only from this point this message has a chance of being learned
        // set new time for learn interval, regardless of whether the learning succeeds
        entity.Comp1.NextLearnInterval = _gameTiming.CurTime + entity.Comp1.LearnCooldown;

        // decide if this message passes the learning chance
        if (!_random.Prob(entity.Comp1.LearnChance))
            return;

        // actually commit this message to memory
        Learn((entity, entity.Comp1), message, source);
    }

    /// <summary>
    /// Actually learn a message and commit it to memory
    /// </summary>
    /// <param name="entity">Entity learning a new word</param>
    /// <param name="message">Message to learn</param>
    /// <param name="source">Source EntityUid of the message</param>
    private void Learn(Entity<ParrotMemoryComponent> entity, string message, EntityUid source)
    {
        // log a low-priority chat type log to the admin logger
        // specifies what message was learnt by what entity, and who taught the message to that entity
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} learned the phrase \"{message}\" from {ToPrettyString(source):speaker}");

        NetUserId? sourceNetUserId = null;
        if (_mind.TryGetMind(source, out _, out var mind))
        {
            sourceNetUserId = mind.UserId;
        }

        var newMemory = new SpeechMemory(sourceNetUserId, message);

        // add a new message if there is space in the memory
        if (entity.Comp.SpeechMemories.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemories.Add(newMemory);
            return;
        }

        // if there's no space in memory, replace something at random
        var replaceIdx = _random.Next(entity.Comp.SpeechMemories.Count);
        entity.Comp.SpeechMemories[replaceIdx] = newMemory;
    }

    /// <summary>
    /// Delete all messages from a specified player on all ParrotMemoryComponents
    /// </summary>
    /// <param name="playerNetUserId">The player of whom to delete messages</param>
    private void DeletePlayerMessages(NetUserId playerNetUserId)
    {
        // query to enumerate all entities with a memorycomponent
        var query = EntityQueryEnumerator<ParrotMemoryComponent>();
        while (query.MoveNext(out _, out var memory))
        {
            DeletePlayerMessages(memory, playerNetUserId);
        }
    }

    /// <summary>
    /// Delete all messages from a specified player on a given ParrotMemoryComponent
    /// </summary>
    /// <param name="memoryComponent">The ParrotMemoryComponent on which to delete messages</param>
    /// <param name="playerNetUserId">The player of whom to delete messages</param>
    private void DeletePlayerMessages(ParrotMemoryComponent memoryComponent, NetUserId playerNetUserId)
    {
        // this is a sort of expensive operation that is hopefully rare and performed on just a few parrots
        // with limited memory
        for (var i = 0; i < memoryComponent.SpeechMemories.Count; i++)
        {
            var memory = memoryComponent.SpeechMemories[i];

            // netuserid may be null if the message was learnt from a non-player entity
            if (memory.NetUserId is null)
                continue;

            // skip if this memory was not learnt from the target user
            if (!memory.NetUserId.Equals(playerNetUserId))
                continue;

            // order isn't important in this list so we can use the faster means of removing
            memoryComponent.SpeechMemories.RemoveSwap(i);
        }
    }
}
