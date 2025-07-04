using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Animals.Components;
using Content.Server.Mind;
using Content.Server.Radio;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
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
public sealed partial class ParrotMemorySystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EraseEvent>(OnErase);

        SubscribeLocalEvent<ParrotMemoryComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeLocalEvent<ParrotListenerComponent, MapInitEvent>(ListenerOnMapInit);

        SubscribeLocalEvent<ParrotListenerComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotListenerComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotMemoryComponent, TryVocalizeEvent>(OnTryVocalize);
    }

    private void OnErase(ref EraseEvent args)
    {
        DeletePlayerMessages(args.PlayerNetUserId);
    }

    private void OnGetVerbs(Entity<ParrotMemoryComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        // limit this to admins
        if (!_admin.IsAdmin(args.User))
            return;

        // simple verb that just clears the memory list
        var clearMemoryVerb = new Verb()
        {
            Text = Loc.GetString("parrot-verb-clear-memory"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/clear-parrot.png")),
            Act = () => entity.Comp.SpeechMemories.Clear(),
        };

        args.Verbs.Add(clearMemoryVerb);
    }

    private void ListenerOnMapInit(Entity<ParrotListenerComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotListenerComponent it really ought to have an ActiveListenerComponent
        if (!HasComp<ActiveListenerComponent>(entity))
            Log.Error($"Entity {ToPrettyString(entity)} has a ParrotListenerComponent but was not given a ActiveListenerComponent");
    }

    private void OnListen(Entity<ParrotListenerComponent> entity, ref ListenEvent args)
    {
        // return if whitelist is not null or fails to pass
        if (!_whitelist.IsWhitelistPassOrNull(entity.Comp.Whitelist, args.Source))
            return;

        // return if blacklist is not null or passes
        if (!_whitelist.IsBlacklistFailOrNull(entity.Comp.Blacklist, args.Source))
            return;

        TryLearn(entity.Owner, args.Message, args.Source);
    }

    private void OnRadioReceive(Entity<ParrotListenerComponent> entity, ref RadioReceiveEvent args)
    {
        // return if whitelist is not null or fails to pass
        if (!_whitelist.IsWhitelistPassOrNull(entity.Comp.Whitelist, args.MessageSource))
            return;

        // return if blacklist is not null or passes
        if (!_whitelist.IsBlacklistFailOrNull(entity.Comp.Blacklist, args.MessageSource))
            return;

        TryLearn(entity.Owner, args.Message, args.MessageSource);
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
    public void TryLearn(Entity<ParrotMemoryComponent?> entity, string incomingMessage, EntityUid source)
    {
        // ignore yourself
        if (source.Equals(entity))
            return;

        // learning requires a memory
        if (!Resolve<ParrotMemoryComponent>(entity, ref entity.Comp))
            return;

        // can't learn when unconscious
        if (_mobState.IsIncapacitated(entity))
            return;

        // can't learn too soon after having already learnt something else
        if (_gameTiming.CurTime < entity.Comp.NextLearnInterval)
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
        if (message.Length < entity.Comp.MinEntryLength || message.Length > entity.Comp.MaxEntryLength)
            return;

        // only from this point this message has a chance of being learned
        // set new time for learn interval, regardless of whether the learning succeeds
        entity.Comp.NextLearnInterval = _gameTiming.CurTime + entity.Comp.LearnCooldown;

        // decide if this message passes the learning chance
        if (!_random.Prob(entity.Comp.LearnChance))
            return;

        // actually commit this message to memory
        Learn((entity, entity.Comp), message, source);
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
        for (var i = 0; i < memoryComponent.SpeechMemories.Count; i++)
        {
            var memory = memoryComponent.SpeechMemories[i];

            if (memory.NetUserId is null)
                continue;

            if (!memory.NetUserId.Equals(playerNetUserId))
                continue;

            memoryComponent.SpeechMemories.RemoveSwap(i);
        }
    }
}
