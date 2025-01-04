using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.Mind;
using Content.Server.Poly.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Silicons.Laws;
using Content.Server.Speech;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Poly.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class PolySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IonStormSystem _ionStormSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private ISawmill _sawmill = default!;
    private readonly string _earSlot = SlotFlags.EARS.ToString().ToLower();

    public override void Initialize()
    {
        SubscribeLocalEvent<PolyComponent, RadioReceiveEvent>(OnRadioReceive);
        SubscribeLocalEvent<PolyComponent, ClothingDidEquippedEvent>(OnClothingDidEquippedEvent);
        SubscribeLocalEvent<PolyComponent, ClothingDidUnequippedEvent>(OnClothingDidUnequippedEvent);
        SubscribeLocalEvent<PolyComponent, MapInitEvent>(OnMapInit, after: [typeof(LoadoutSystem)]);
        SubscribeLocalEvent<PolyComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<PolyComponent, RoundEndMessageEvent>(OnRoundEnd);

        _sawmill = _logManager.GetSawmill("polyparrot");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var bird in EntityQuery<PolyComponent>())
        {
            if (_timing.CurTime < bird.BarkAccumulator + bird.BarkTime)
                continue;

            bird.BarkAccumulator = _timing.CurTime;

            SayRandomSentence(new Entity<PolyComponent>(bird.Owner, bird));

            if (bird.SpeechBuffer.Count == 0)
                _ = FillBuffer(new Entity<PolyComponent>(bird.Owner, bird)); // Uh uh!! Big hack cause idk what I'm doing
        }
    }

    /// <summary>
    /// Poly is unable to directly use a headset, so we use this to sync the state of his built-in radio with the headset.
    /// Yes, the bird has a built-in radio. Deal with it.
    /// </summary>
    private void OnClothingDidEquippedEvent(EntityUid uid, PolyComponent component, ref ClothingDidEquippedEvent args)
    {
        if (args.Clothing.Comp.InSlot != _earSlot)
            return;

        if (!TryComp<ActiveRadioComponent>(args.Clothing.Owner, out var radio))
            return;

        component.Headset = args.Clothing.Owner;
        EnsureComp<ActiveRadioComponent>(uid).Channels = [..radio.Channels];
        //EnsureComp<IntrinsicRadioTransmitterComponent>(uid).Channels = [..radio.Channels];
    }

    private void OnClothingDidUnequippedEvent(EntityUid uid, PolyComponent component, ref ClothingDidUnequippedEvent args)
    {
        if (args.Clothing.Comp.InSlot != _earSlot)
            return;

        if (!TryComp<ActiveRadioComponent>(args.Clothing.Owner, out _))
            return;

        component.Headset = EntityUid.Invalid;
        RemComp<ActiveRadioComponent>(uid);
        //if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
        //    transmitter.Channels.Clear();
    }

    private void OnMapInit(EntityUid uid, PolyComponent component, ref MapInitEvent args)
    {
        // Init Poly's speech buffer
        _ = FillBuffer(new Entity<PolyComponent>(uid, component));

        // Check if idiot bird is wearing a headset
        if (!_inventorySystem.TryGetSlotEntity(uid, _earSlot, out var ears))
            return;

        if (!TryComp<ActiveRadioComponent>(ears, out var radio))
            return;

        component.Headset = (EntityUid)ears;
        EnsureComp<ActiveRadioComponent>(uid).Channels = [..radio.Channels];
        //EnsureComp<IntrinsicRadioTransmitterComponent>(uid).Channels = [..radio.Channels];
    }

    private void OnRadioReceive(EntityUid uid, PolyComponent component, RadioReceiveEvent args)
    {
        var message = args.Message;
        var author = args.MessageSource;
        var channel = args.Channel.ID;

        // Print it out for testing
        _sawmill.Info($"Received message: \"{message}\" on channel {args.Channel.LocalizedName}");

        LearnSentence(new Entity<PolyComponent>(uid, component),
            component.RadioLearnProbability,
            channel,
            message,
            author);
    }

    private void OnListen(EntityUid uid, PolyComponent component, ListenEvent args)
    {
        var message = args.Message;
        var author = args.Source;

        // Print it out for testing
        _sawmill.Info($"Received local: {message}");

        LearnSentence(new Entity<PolyComponent>(uid, component),
            component.LocalLearnProbability,
            "local",
            message,
            author);
    }

    private async void OnRoundEnd(EntityUid uid, PolyComponent component, RoundEndMessageEvent args)
    {
        if (component.SavedMemory)
            return;

        foreach (var (channel, sentence, guid) in component.Memory)
        {
            await _db.InsertPolyMemory(channel, sentence, guid);
            _sawmill.Info($"Saved new memory: {sentence}");
        }

        _sawmill.Info("Memory saved to database");
        component.Memory.Clear();
        component.SavedMemory = true;
    }

    private void LearnSentence(Entity<PolyComponent> poly,
        float probability,
        string channel,
        string sentence,
        EntityUid author)
    {
        if (_timing.CurTime < poly.Comp.StateTime + poly.Comp.LearnCooldown)
        {
            _sawmill.Debug("Poly is still on cooldown");
            return;
        }

        poly.Comp.StateTime = _timing.CurTime;

        if (sentence.Contains("Poly")) // Egotistical bird
            probability += 0.05f;

        if (!_robustRandom.Prob(probability))
        {
            _sawmill.Debug("Poly didn't learn this time");
            return;
        }

        _sawmill.Debug("Poly learned a new sentence");

        Guid? authorGuid = null;
        // I hate this function, surely theres a method I need to look for lol
        if (TryComp<MindContainerComponent>(author, out var mind))
        {
            if (TryComp<MindComponent>(mind.Mind, out var mindComp))
                authorGuid = mindComp.UserId;
        }

        if (sentence.Length > 255)
                sentence = sentence[..255];

        poly.Comp.Memory.Add((channel, sentence, authorGuid));
        _adminLogManager.Add(LogType.PolyLearned, LogImpact.Medium, $"{ToPrettyString(poly.Owner)} learned new sentence \"{sentence}\" from {ToPrettyString(author):player}");
    }

    private async Task FillBuffer(Entity<PolyComponent> poly)
    {
        var sentences = await _db.PopulatePolyBuffer(); // This might not return anything if the database is empty, so TODO: add check for this
        poly.Comp.SpeechBuffer = new(sentences.Select(s => (s.Channel, s.Phrase)));
    }

    private void SayRandomSentence(Entity<PolyComponent> poly)
    {
        var (channel, sentence, _) = PickRandomSentence(poly);

        // If the channel is not local, try to resolve the prototype
        _sawmill.Debug($"Channel is {channel}");
        if (_prototypeManager.TryIndex<RadioChannelPrototype>(channel, out var prototype) &&
            TryComp<ActiveRadioComponent>(poly.Owner, out var transmitter) &&
            transmitter.Channels.Contains(prototype.ID) &&
            poly.Comp.Headset.Valid)
        {
            _sawmill.Info($"Poly said on radio: {sentence}");
            _chatSystem.TrySendInGameICMessage(poly.Owner, sentence, InGameICChatType.Whisper, ChatTransmitRange.Normal);

            var cleanedMessage = _chatSystem.TransformSpeech(poly.Owner, sentence);
            cleanedMessage = _chatSystem.SanitizeInGameICMessage(poly.Owner, cleanedMessage, out _);
            _radioSystem.SendRadioMessage(poly.Owner, cleanedMessage , prototype, poly.Owner); // Poly the radio
            return;
        }

        _sawmill.Info($"Poly said local: {sentence}");

        _chatSystem.TrySendInGameICMessage(poly.Owner, sentence, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private (string channel, string sentence, Guid? author) PickRandomSentence(Entity<PolyComponent> poly)
    {
        // Poly can use a few sources for sentences, below are the options with their odds
        // 1. Poly's memory (50%)
        // 2. The database (35%)
        // 3. Borg ion laws (15%)

        if (_robustRandom.Prob(0.5f) && poly.Comp.Memory.Count > 0)
        {
            var index = _robustRandom.Next(poly.Comp.Memory.Count);
            var sentence = poly.Comp.Memory.ElementAt(index);
            return sentence;
        }

        if (_robustRandom.Prob(0.35f))
        {
            // Pick a random speech from the buffer
            if (poly.Comp.SpeechBuffer.Count > 0)
            {
                // The buffer was already randomized when we pulled it, so we can just pop the first element
                var sentence = poly.Comp.SpeechBuffer.First();
                poly.Comp.SpeechBuffer.Remove(sentence);
                return (sentence.Channel, sentence.Sentence, null);
            }
        }

        var law = _ionStormSystem.GenerateLaw().ToLower(); // We lowercase it here, the chat system should handle formatting it
        var channel = _robustRandom.Pick(new[] {"Local", "Engineering", "Common"});

        return (channel, law, null);
    }
}
