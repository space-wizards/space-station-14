using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Inventory;
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
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ParrotAccentSystem _parrotAccent = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParrotComponent, ComponentInit>(OnParrotInit);

        SubscribeLocalEvent<ParrotComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotComponent, ParrotLearnEvent>(OnLearn);
        SubscribeLocalEvent<ParrotComponent, ParrotSpeakEvent>(OnSpeak);
    }

    private void OnParrotInit(Entity<ParrotComponent> entity, ref ComponentInit args)
    {
        StartListening(entity);
    }

    private void StartListening(Entity<ParrotComponent> entity)
    {
        if (entity.Comp.Listening)
            return;

        entity.Comp.Listening = true;

        EnsureComp<ActiveListenerComponent>(entity);
    }

    private void StartListening(EntityUid uid, ParrotComponent parrot)
    {
        if (parrot.Listening)
            return;

        parrot.Listening = true;

        EnsureComp<ActiveListenerComponent>(uid);
    }

    private void StopListening(Entity<ParrotComponent> entity)
    {
        if (!entity.Comp.Listening)
            return;

        entity.Comp.Listening = false;

        RemCompDeferred<ActiveListenerComponent>(entity);
    }

    private void OnListen(Entity<ParrotComponent> entity, ref ListenEvent args)
    {
        Learn(entity, args.Message, args.Source);
    }

    private void OnRadioReceive(Entity<ParrotComponent> entity, ref RadioReceiveEvent args)
    {
        Learn(entity, args.Message, args.MessageSource);
    }

    private void Learn(Entity<ParrotComponent> entity, string incomingMessage, EntityUid source)
    {
        // too early to learn
        if (_random.NextFloat() > entity.Comp.LearnChance)
            return;

        // ignore speakers with ParrotComponent or things get silly
        if (HasComp<ParrotComponent>(source))
            return;

        var message = incomingMessage.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        // message length out of bounds
        if (message.Length < entity.Comp.MinEntryLength || message.Length > entity.Comp.MaxEntryLength)
            return;

        // actually fire the learning event
        var learnEvent = new ParrotLearnEvent(message, source);
        RaiseLocalEvent(entity, ref learnEvent);
    }

    private void OnLearn(Entity<ParrotComponent> entity, ref ParrotLearnEvent args)
    {
        // set new time for learn interval
        entity.Comp.NextLearnInterval = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.LearnCooldown);

        // reset next speak interval if this is the first thing the parrot learnt
        if (entity.Comp.SpeechMemory.Count == 0)
        {
            var intervalSeconds = _random.NextFloat(entity.Comp.MinSpeakInterval, entity.Comp.MaxSpeakInterval);
            var randomSpeakInterval = TimeSpan.FromSeconds(intervalSeconds);

            entity.Comp.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;
        }

        var tryLearnEvent = new ParrotTryLearnEvent(args.Message, args.Teacher);
        RaiseLocalEvent(entity, ref tryLearnEvent);

        // expect to be canceled if dead or something
        // potentially also if poly has more elaborate learning stuff
        if (tryLearnEvent.Cancelled)
            return;

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} learned the phrase \"{args.Message}\" from {ToPrettyString(args.Teacher):speaker}");

        // can fit more in peanut brain :)
        if (entity.Comp.SpeechMemory.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemory.Add(args.Message);
        }
        // too much in brain. remove something :(
        else
        {
            var replaceIdx = _random.Next(entity.Comp.SpeechMemory.Count);
            entity.Comp.SpeechMemory[replaceIdx] = args.Message;
        }


        if (entity.Comp.LearnCooldown == 0.0f)
            return;

        StopListening(entity);
    }

    private void OnSpeak(Entity<ParrotComponent> entity, ref ParrotSpeakEvent args)
    {
        if (!HasComp<SpeechComponent>(entity))
            return;

        var intervalSeconds = _random.NextFloat(entity.Comp.MinSpeakInterval, entity.Comp.MaxSpeakInterval);
        var randomSpeakInterval = TimeSpan.FromSeconds(intervalSeconds);

        entity.Comp.NextSpeakInterval = _gameTiming.CurTime + randomSpeakInterval;

        var message = _random.Pick(entity.Comp.SpeechMemory);

        // choose between radio and chat
        if (_random.Prob(0.5f))
        {
            _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }
        else
        {
            TrySpeakRadio(entity, message);
        }

    }

    private bool TrySpeakRadio(Entity<ParrotComponent> entity, string message)
    {
        if (!_inventory.TryGetSlotEntity(entity, "ears", out var item))
            return false;

        if (!TryComp<ActiveRadioComponent>(item.Value, out var radio))
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
        _radio.SendRadioMessage(entity, message, _proto.Index<RadioChannelPrototype>(channel), item.Value);

        return true;
    }

    public override void Update(float frameTime)
    {
        var currentGameTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<ParrotComponent>();
        while (query.MoveNext(out var uid, out var parrot))
        {
            if (!parrot.Listening && currentGameTime > parrot.NextLearnInterval)
                StartListening(uid, parrot);

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

[ByRefEvent]
public record struct ParrotTryLearnEvent(string Message, EntityUid Teacher)
{
    public bool Cancelled = false;
};

[ByRefEvent]
public readonly record struct ParrotLearnEvent(string Message, EntityUid Teacher);

[ByRefEvent]
public readonly record struct ParrotSpeakEvent();
