using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

public sealed partial class ParrotSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ParrotAccentSystem _parrotAccent = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParrotComponent, ComponentInit>(OnParrotInit, after: [typeof(LoadoutSystem)]);

        SubscribeLocalEvent<ParrotComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotComponent, ClothingDidEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<ParrotComponent, ClothingDidUnequippedEvent>(OnClothingUnequipped);

        SubscribeLocalEvent<ParrotComponent, ParrotLearnEvent>(OnLearn);
        SubscribeLocalEvent<ParrotComponent, ParrotSpeakEvent>(OnSpeak);
    }

    private void OnParrotInit(Entity<ParrotComponent> entity, ref ComponentInit args)
    {
        EnsureComp<ActiveListenerComponent>(entity).Range = entity.Comp.ListenRange;

        // get starting radio on entity
        if (!_inventory.TryGetSlotEntity(entity, "ears", out var item))
            return;

        if (!TryComp<ActiveRadioComponent>(item.Value, out var radio))
            return;

        var radioEntity = new Entity<ActiveRadioComponent>(item.Value, radio);

        entity.Comp.ActiveRadio = radioEntity;

        CopyParrotRadioChannels(entity, radioEntity);
    }

    /// <summary>
    /// Listen callback for when a nearby chat message is received
    /// </summary>
    private void OnListen(Entity<ParrotComponent> entity, ref ListenEvent args)
    {
        TryLearn(entity, args.Message, args.Source);
    }

    /// <summary>
    /// Callback for when a radio message is received
    /// </summary>
    private void OnRadioReceive(Entity<ParrotComponent> entity, ref RadioReceiveEvent args)
    {
        TryLearn(entity, args.Message, args.MessageSource);
    }

    private void OnClothingEquipped(Entity<ParrotComponent> entity, ref ClothingDidEquippedEvent args)
    {
        if (!TryComp<ActiveRadioComponent>(args.Clothing, out var radio))
            return;

        var newActiveRadio = new Entity<ActiveRadioComponent>(args.Clothing.Owner, radio);

        entity.Comp.ActiveRadio = newActiveRadio;

        CopyParrotRadioChannels(entity, newActiveRadio);
    }

    private void OnClothingUnequipped(Entity<ParrotComponent> entity, ref ClothingDidUnequippedEvent args)
    {
        if (args.Clothing != entity.Comp.ActiveRadio)
            return;

        EnsureComp<ActiveRadioComponent>(entity, out var parrotRadio);

        parrotRadio.Channels = [];
    }

    private void CopyParrotRadioChannels(Entity<ParrotComponent> entity, Entity<ActiveRadioComponent> copyFrom)
    {
        EnsureComp<ActiveRadioComponent>(entity, out var parrotRadio);

        parrotRadio.Channels = copyFrom.Comp.Channels;
    }

    /// <summary>
    /// Try to learn a new message
    /// </summary>
    private void TryLearn(Entity<ParrotComponent> entity, string incomingMessage, EntityUid source)
    {
        // can't learn when crit or dead
        if (_mobState.IsIncapacitated(entity))
            return;

        // ignore yourself
        if (source.Equals(entity))
            return;

        // fail to learn "Urist mctider is stabbing me". Succeed in learning "nuh uh"
        if (!_random.Prob(entity.Comp.LearnChance))
            return;

        // ignore speakers with ParrotComponent or things get silly
        // if (HasComp<ParrotComponent>(source))
        //     return;

        var message = incomingMessage.Trim();

        // ignore messages containing tildes. This is a crude way to ignore whispers or people talking on radio
        // near a parrot
        if (message.Contains('~'))
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        // message length out of bounds
        if (message.Length < entity.Comp.MinEntryLength || message.Length > entity.Comp.MaxEntryLength)
            return;

        // actually fire the learning event
        var learnEvent = new ParrotLearnEvent(message, source);
        RaiseLocalEvent(entity, ref learnEvent);
    }

    /// <summary>
    /// Callback for when a parrot succeeds in learning a new message
    /// </summary>
    private void OnLearn(Entity<ParrotComponent> entity, ref ParrotLearnEvent args)
    {
        // set new time for learn interval
        entity.Comp.NextLearnInterval = _gameTiming.CurTime + entity.Comp.LearnCooldown;

        // reset next speak interval if this is the first thing the parrot learnt
        if (entity.Comp.SpeechMemory.Count == 0)
        {
            var intervalSeconds = _random.NextFloat(entity.Comp.MinSpeakInterval, entity.Comp.MaxSpeakInterval);
            var randomSpeakInterval = TimeSpan.FromSeconds(intervalSeconds);

            entity.Comp.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;
        }

        // raise an event that we're trying to learn something
        var tryLearnEvent = new ParrotTryLearnEvent(args.Message, args.Teacher);
        RaiseLocalEvent(entity, ref tryLearnEvent);

        if (tryLearnEvent.Cancelled)
            return;

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} learned the phrase \"{args.Message}\" from {ToPrettyString(args.Teacher):speaker}");

        // can fit more in peanut brain :)
        if (entity.Comp.SpeechMemory.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemory.Add(args.Message);
            return;
        }

        // too much in brain. remove something :(
        var replaceIdx = _random.Next(entity.Comp.SpeechMemory.Count);
        entity.Comp.SpeechMemory[replaceIdx] = args.Message;
    }

    /// <summary>
    /// Callback for when a parrot wants to say something
    /// </summary>
    private void OnSpeak(Entity<ParrotComponent> entity, ref ParrotSpeakEvent args)
    {
        // can't talk if you don't have a speech component
        if (!TryComp<SpeechComponent>(entity, out var speech))
            return;

        // or if the speech component is disabled
        if (!speech.Enabled)
            return;

        // set a new time for the speak interval
        var intervalSeconds = _random.NextFloat(entity.Comp.MinSpeakInterval, entity.Comp.MaxSpeakInterval);
        var randomSpeakInterval = TimeSpan.FromSeconds(intervalSeconds);

        entity.Comp.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;

        // get a random message from the memory
        var message = _random.Pick(entity.Comp.SpeechMemory);

        // choice between radio and chat
        // if talking on the radio doesn't work, will fall back to regular chat
        if (_random.Prob(entity.Comp.RadioAttemptChance) && TrySpeakRadio(entity, message))
            return;

        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or some other weird stuff happens
    /// </summary>
    private bool TrySpeakRadio(Entity<ParrotComponent> entity, string message)
    {
        if (entity.Comp.ActiveRadio is null)
            return false;

        if (!TryComp<ActiveRadioComponent>(entity.Comp.ActiveRadio, out var radio))
            return false;

        // accentuate for radio. squawk. this isn't great, obviously
        // also results in a different accent result between chat parrot and radio parrot
        // if this ends up being such that the radio is chosen alongside chat
        if (TryComp<ParrotAccentComponent>(entity, out var accentComponent))
        {
            var accentedEntity = new Entity<ParrotAccentComponent>(entity, accentComponent);
            message = _parrotAccent.Accentuate(accentedEntity, message);
        }

        // choose random channel
        var channel = _random.Pick(radio.Channels);
        _radio.SendRadioMessage(entity, message, _proto.Index<RadioChannelPrototype>(channel), entity);

        return true;
    }

    public override void Update(float frameTime)
    {
        var currentGameTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<ParrotComponent>();
        while (query.MoveNext(out var uid, out var parrot))
        {
            // can't talk when crit or dead
            if (_mobState.IsIncapacitated(uid))
                return;

            // no need to continue to speak if there is nothing to say
            if (parrot.SpeechMemory.Count == 0)
                continue;

            // return if too early to speak
            if (currentGameTime < parrot.NextSpeakInterval)
                continue;

            var speakEvent = new ParrotSpeakEvent();
            RaiseLocalEvent<ParrotSpeakEvent>(uid, ref speakEvent);
        }
    }
}

/// <summary>
/// Event that is fired when a parrot tries to learn, after a successful learn roll but before anything is added to memory
/// </summary>
[ByRefEvent]
public record struct ParrotTryLearnEvent(string Message, EntityUid Teacher)
{
    public bool Cancelled = false;
};

/// <summary>
/// Event that is fired when a parrot has learned a new message
/// </summary>
[ByRefEvent]
public readonly record struct ParrotLearnEvent(string Message, EntityUid Teacher);

/// <summary>
/// Event that is fired when a parrot tries to speak
/// </summary>
[ByRefEvent]
public readonly record struct ParrotSpeakEvent();
