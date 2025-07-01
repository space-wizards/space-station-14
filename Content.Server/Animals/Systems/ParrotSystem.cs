using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// ParrotSystem handles parroting. That is, speaking random learnt messages from memory.
/// At minimum, an entity requires a ParrotSpeakerComponent and a ParrotMemoryComponent for this system to be active.
/// Without a ParrotMemoryComponent, nothing will ever be said by an entity.
/// ParrotMemoryComponent gets filled when ParrotListenerComponent is present on the entity.
/// With a ParrotListenerComponent, entities listen to nearby local IC chat to fill memory.
/// </summary>
public sealed partial class ParrotSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotListenerComponent, MapInitEvent>(ListenerOnMapInit);
        SubscribeLocalEvent<ParrotListenerComponent, ListenEvent>(OnListen);
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

    /// <summary>
    /// Try to learn a new message, returning early if this entity cannot learn a new message,
    /// the message doesn't pass certain checks, or the chance for learning a new message fails
    /// </summary>
    /// <param name="entity">Entity learning a new word</param>
    /// <param name="incomingMessage">Message to learn</param>
    /// <param name="source">Source EntityUid of the message</param>
    public void TryLearn(Entity<ParrotMemoryComponent?> entity, string incomingMessage, EntityUid source)
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

        // Return if a source has a ParrotSpeakerComponent, this entity has a ParrotListenerComponent, and that
        // component is set to ignore ParrotSpeakers.
        // used to prevent accent parroting from getting out of hand
        if (
            HasComp<ParrotSpeakerComponent>(source)
            && TryComp<ParrotListenerComponent>(entity, out var parrotListener)
            && parrotListener.IgnoreParrotSpeakers)
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
        // reset next speak interval if the entity has a ParrotSpeakComponent and this is the first thing it learns
        // this is done so that a parrot doesn't speak the moment it learns something
        if (TryComp<ParrotSpeakerComponent>(entity, out var speakerComponent) && entity.Comp.SpeechMemory.Count == 0)
        {
            var randomSpeakInterval = _random.Next(speakerComponent.MinSpeakInterval, speakerComponent.MaxSpeakInterval);
            speakerComponent.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;
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

    private void TrySpeak(Entity<ParrotSpeakerComponent, ParrotMemoryComponent> entity)
    {
        var memory = entity.Comp2;

        // return if the entity can't speak at all
        if (!_actionBlocker.CanSpeak(entity))
            return;

        // no need to continue to speak if there is nothing to say
        if (memory.SpeechMemory.Count == 0)
            return;

        Speak(entity);
    }

    /// <summary>
    /// Actually say something.
    /// Expects an entity to have a ParrotSpeakerComponent and a ParrotMemoryComponent at minimum
    /// </summary>
    private void Speak(Entity<ParrotSpeakerComponent, ParrotMemoryComponent> entity)
    {

        var memory = entity.Comp2;

        // get a random message from the memory
        var message = _random.Pick(memory.SpeechMemory);

        // raise a parrotSpeakEvent
        var speakEvent = new ParrotSpeakEvent(message);
        RaiseLocalEvent(entity.Owner, ref speakEvent);

        // if the event is handled, don't try speaking
        if (speakEvent.Handled)
            return;

        // default to local chat if no other system speaks on this parrots behalf
        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // get current game time for delay
        var currentGameTime = _gameTiming.CurTime;

        // query to get all components with parrot memory and speaker
        var query = EntityQueryEnumerator<ParrotMemoryComponent, ParrotSpeakerComponent>();
        while (query.MoveNext(out var uid, out var parrotMemory, out var parrotSpeaker))
        {
            // go to next entity if it is too early for this one to speak
            if (currentGameTime < parrotSpeaker.NextSpeakInterval)
                continue;

            // set a new time for the speak interval, regardless of whether speaking works
            var randomSpeakInterval = _random.Next(parrotSpeaker.MinSpeakInterval, parrotSpeaker.MaxSpeakInterval);
            parrotSpeaker.NextSpeakInterval += randomSpeakInterval;

            // if an admin updates the speak interval to be immediate, this loop will spam messages until the
            // nextspeakinterval catches up with the current game time. Prevent this from happening
            if (parrotSpeaker.NextSpeakInterval < _gameTiming.CurTime)
                parrotSpeaker.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;

            // try to speak
            TrySpeak((uid, parrotSpeaker, parrotMemory));
        }
    }
}

[ByRefEvent]
public record struct ParrotSpeakEvent(string Message, bool Handled = false);
