using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// ParrotSystem handles parroting. That is, speaking random learnt messages from memory.
/// At minimum, an entity requires a ParrotSpeakerComponent and a ParrotMemoryComponent for this system to be active.
/// Without a ParrotMemoryComponent, nothing will ever be said by an entity.
/// ParrotMemoryComponent gets filled when ParrotListenerComponent is present on the entity.
/// With a ParrotListenerComponent, entities listen to nearby local IC chat to fill memory.
/// If an entity also has a ParrotRadioComponent, it will also listen for messages on radio channels of radios it has
/// equipped. It will also have a chance to say things on radio channels of radios it has equipped.
/// </summary>
public sealed partial class ParrotSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = null!;
    [Dependency] private readonly ChatSystem _chat = null!;
    [Dependency] private readonly IAdminLogManager _adminLogger = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IPrototypeManager _proto = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly MobStateSystem _mobState = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotListenerComponent, MapInitEvent>(ListenerOnMapInit);
        SubscribeLocalEvent<ParrotListenerComponent, ListenEvent>(OnListen);

        SubscribeLocalEvent<ParrotRadioComponent, MapInitEvent>(RadioOnMapInit);
        SubscribeLocalEvent<ParrotRadioComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotRadioComponent, ClothingDidEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<ParrotRadioComponent, ClothingDidUnequippedEvent>(OnClothingUnequipped);
    }

    /// <summary>
    /// Fired after map initialization. Used to ensure the parrot has an ActiveListenerComponent if it has a
    /// ParrotListenerComponent
    /// </summary>
    private void ListenerOnMapInit(Entity<ParrotListenerComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotListenerComponent it really ought to have an ActiveListenerComponent
        EnsureComp<ActiveListenerComponent>(entity);
    }

    /// <summary>
    /// Callback for when a nearby chat message is received
    /// </summary>
    private void OnListen(Entity<ParrotListenerComponent> entity, ref ListenEvent args)
    {
        TryLearn(entity.Owner, args.Message, args.Source);
    }

    /// <summary>
    /// Fired after map initialization. Used to ensure the parrot has an ActiveRadioComponent if it has a
    /// ParrotRadioComponent. Updates radio channels
    /// </summary>
    private void RadioOnMapInit(Entity<ParrotRadioComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotRadioComponent it really ought to have an ActiveRadioComponent
        var activeRadio = EnsureComp<ActiveRadioComponent>(entity);

        UpdateParrotRadioChannels((entity, activeRadio));
    }

    /// <summary>
    /// Callback for when a radio message is received
    /// </summary>
    private void OnRadioReceive(Entity<ParrotRadioComponent> entity, ref RadioReceiveEvent args)
    {
        TryLearn(entity.Owner, args.Message, args.MessageSource);
    }

    /// <summary>
    /// Callback for when this entity equips clothing or has clothing equipped by something
    /// Used to update the ActiveRadio entities a parrot with a ParrotRadioComponent uses to listen to radios and
    /// talk on radio channels
    /// </summary>
    private void OnClothingEquipped(Entity<ParrotRadioComponent> entity, ref ClothingDidEquippedEvent args)
    {
        // only care if the equipped clothing item has an ActiveRadioComponent
        if (!HasComp<ActiveRadioComponent>(args.Clothing))
            return;

        entity.Comp.ActiveRadioEntities.Add(args.Clothing.Owner);

        // update active radio channels
        UpdateParrotRadioChannels(entity.Owner);
    }

    /// <summary>
    /// Called if this entity unequipped clothing or has clothing unequipped by something
    /// Used to update the ActiveRadio entities a parrot with a ParrotRadioComponent uses to listen to radios and
    /// talk on radio channels
    /// </summary>
    private void OnClothingUnequipped(Entity<ParrotRadioComponent> entity, ref ClothingDidUnequippedEvent args)
    {
        // try to remove this item from the active radio entities list
        // if this returns false, the item wasn't found so it was never a radio we cared about, quit early
        if (!entity.Comp.ActiveRadioEntities.Remove(args.Clothing.Owner))
            return;

        // update active radio channels
        UpdateParrotRadioChannels(entity.Owner);
    }

    /// <summary>
    /// Copies all radio channels from equipped radios to the ActiveRadioComponent of an entity
    /// </summary>
    public void UpdateParrotRadioChannels(Entity<ActiveRadioComponent?, ParrotRadioComponent?> entity)
    {
        if (!Resolve<ActiveRadioComponent>(entity, ref entity.Comp1))
            return;

        if (!Resolve<ParrotRadioComponent>(entity, ref entity.Comp2))
            return;

        // clear all channels first
        entity.Comp1.Channels.Clear();

        // quit early if there are no ActiveRadios on the ParrotRadioComponent
        if (entity.Comp2.ActiveRadioEntities.Count == 0)
            return;

        // loop through ActiveRadios in inventory to (re-)add channels
        foreach (var radio in entity.Comp2.ActiveRadioEntities)
        {
            // if for whatever reason this entity does not have an ActiveRadioComponent, skip it
            if (!TryComp<ActiveRadioComponent>(radio, out var activeRadioComponent))
                continue;

            // add them to the channels on the ActiveRadioComponent on the entity
            entity.Comp1.Channels.UnionWith(activeRadioComponent.Channels);
        }
    }

    /// <summary>
    /// Try to learn a new chat or radio message, returning early if this entity cannot learn a new message,
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
    /// If an entity also has a ParrotRadioComponent, it will have a chance to speak on the radio
    /// </summary>
    private void Speak(Entity<ParrotSpeakerComponent, ParrotMemoryComponent> entity)
    {

        var memory = entity.Comp2;

        // get a random message from the memory
        var message = _random.Pick(memory.SpeechMemory);

        // choice between radio and chat
        // see if the entity has a ParrotRadioComponent and whether it passes the radio attempt chance
        if (TryComp<ParrotRadioComponent>(entity, out var radio) && _random.Prob(radio.RadioAttemptChance))
        {
            // try speaking on the radio. If this succeeds, return early
            if (TrySpeakRadio(new Entity<ParrotRadioComponent>(entity, radio), message))
                return;
        }

        // if chance to speak on radio does not pass or speaking on the radio fails for whatever reason, use chat
        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or talking on radio fails somehow
    /// </summary>
    /// <param name="entity">Entity to try and make speak on the radio</param>
    /// <param name="message">Message to speak </param>
    /// <returns></returns>
    private bool TrySpeakRadio(Entity<ParrotRadioComponent> entity, string message)
    {
        // return if this entity does not have an ActiveRadioComponent. Should never happen
        if (!TryComp<ActiveRadioComponent>(entity, out var radio))
            return false;

        // return if this entity's ActiveRadioComponent contains no channels
        if (radio.Channels.Count == 0)
            return false;

        // choose random channel
        var channel = _random.Pick(radio.Channels);
        var channelPrefix = _proto.Index<RadioChannelPrototype>(channel).KeyCode;

        _chat.TrySendInGameICMessage(
            entity,
            $"{SharedChatSystem.RadioChannelPrefix}{channelPrefix} {message}",
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);

        return true;
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
