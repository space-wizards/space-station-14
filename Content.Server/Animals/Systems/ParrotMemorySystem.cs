using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Radio;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.Vocalization.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// The ParrotMemorySystem handles remembering messages received through local chat (activelistener) or a radio
/// (radiovocalizer) and stores them in a list. When an entity with a VocalizerComponent attempts to vocalize, this will
/// try to set the message from memory.
/// </summary>
public sealed partial class ParrotMemorySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotListenerComponent, MapInitEvent>(ListenerOnMapInit);

        SubscribeLocalEvent<ParrotListenerComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RadioVocalizerComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotMemoryComponent, TryVocalizeEvent>(OnTryVocalize);
    }

    private void ListenerOnMapInit(Entity<ParrotListenerComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotListenerComponent it really ought to have an ActiveListenerComponent
        EnsureComp<ActiveListenerComponent>(entity);
    }

    private void OnListen(Entity<ParrotListenerComponent> entity, ref ListenEvent args)
    {
        TryLearn(entity.Owner, args.Message, args.Source);
    }

    private void OnRadioReceive(Entity<RadioVocalizerComponent> entity, ref RadioReceiveEvent args)
    {
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

        // if there are no messages, return
        if (entity.Comp.SpeechMemory.Count == 0)
            return;

        // get a random message from the memory
        var message = _random.Pick(entity.Comp.SpeechMemory);

        args.Message = message;
        args.Handled = true;
    }

    /// <summary>
    /// Try to learn a new message, returning early if this entity cannot learn a new message,
    /// the message doesn't pass certain checks, or the chance for learning a new message fails
    /// </summary>
    /// <param name="entity">Entity learning a new word</param>
    /// <param name="incomingMessage">Message to learn</param>
    /// <param name="source">Source EntityUid of the message</param>
    private void TryLearn(Entity<ParrotMemoryComponent?> entity, string incomingMessage, EntityUid source)
    {
        // learning requires a memory
        if (!Resolve<ParrotMemoryComponent>(entity, ref entity.Comp))
            return;

        // can't learn when unconscious
        if (_mobState.IsIncapacitated(entity))
            return;

        // can't learn too soon after having already learnt something else
        if (_gameTiming.CurTime < entity.Comp.NextLearnInterval)
            return;

        // ignore yourself
        if (source.Equals(entity))
            return;

        // Return if a source has a ParrotListenerComponent, this entity has a ParrotListenerComponent, and the latter
        // component is set to ignore ParrotSpeakers.
        // used to prevent accent parroting from getting out of hand
        if (
            HasComp<ParrotListenerComponent>(source)
            && TryComp<ParrotListenerComponent>(entity, out var parrotListener)
            && parrotListener.IgnoreParrotListeners)
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
        // reset next speak interval if the entity has a VocalizerComponent and this is the first thing it learns
        // this is done so that a parrot doesn't speak the moment it learns something
        if (TryComp<VocalizerComponent>(entity, out var vocalizerComponent) && entity.Comp.SpeechMemory.Count == 0)
        {
            var randomSpeakInterval = _random.Next(vocalizerComponent.MinVocalizeInterval, vocalizerComponent.MaxVocalizeInterval);
            vocalizerComponent.NextVocalizeInterval = _gameTiming.CurTime + randomSpeakInterval;
        }

        // log a low-priority chat type log to the admin logger
        // specifies what message was learnt by what entity, and who taught the message to that entity
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} learned the phrase \"{message}\" from {ToPrettyString(source):speaker}");

        // add a new message if there is space in the memory
        if (entity.Comp.SpeechMemory.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemory.Add(message);
            return;
        }

        // if there's no space in memory, replace something at random
        var replaceIdx = _random.Next(entity.Comp.SpeechMemory.Count);
        entity.Comp.SpeechMemory[replaceIdx] = message;
    }
}
