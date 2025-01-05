using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Poly.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Silicons.Laws;
using Content.Server.Speech;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Poly.Systems;

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
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private ISawmill _sawmill = default!;
    private readonly string _earSlot = SlotFlags.EARS.ToString().ToLower();

    public override void Initialize()
    {
        SubscribeLocalEvent<PolyComponent, RadioReceiveEvent>(OnRadioReceive);
        SubscribeLocalEvent<PolyComponent, ClothingDidEquippedEvent>(OnClothingDidEquippedEvent);
        SubscribeLocalEvent<PolyComponent, ClothingDidUnequippedEvent>(OnClothingDidUnequippedEvent);
        SubscribeLocalEvent<PolyComponent, MapInitEvent>(OnMapInit, after: [typeof(LoadoutSystem)]);
        SubscribeLocalEvent<PolyComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<PolyComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);

        _sawmill = _logManager.GetSawmill("polyparrot");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PolyComponent>();
        while (query.MoveNext(out var uid, out var bird))
        {
            // Is Poly actually still alive???
            if (_mobStateSystem.IsIncapacitated(uid))
                continue;

            if (_timing.CurTime < bird.BarkAccumulator + bird.BarkTime)
                continue;

            bird.BarkAccumulator = _timing.CurTime;

            SayRandomSentence(new Entity<PolyComponent>(uid, bird));

            if (bird.SpeechBuffer.Count == 0 && _configManager.GetCVar(CCVars.PolyPersistantMemory))
                _ = FillBuffer(new Entity<PolyComponent>(uid, bird)); // Uh uh!! Big hack cause idk what I'm doing
        }
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PostRound)
            OnRoundEnd();
    }

    private void OnComponentShutdown(EntityUid uid, PolyComponent component, ref ComponentShutdown args)
    {
        // Poly got fucking gibbed lmao
        if (!_configManager.GetCVar(CCVars.PolyPersistantMemory))
            return;

        _ = FlushMemory(component);
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
    }

    private void OnClothingDidUnequippedEvent(EntityUid uid, PolyComponent component, ref ClothingDidUnequippedEvent args)
    {
        if (args.Clothing.Comp.InSlot != _earSlot)
            return;

        if (!TryComp<ActiveRadioComponent>(args.Clothing.Owner, out _))
            return;

        component.Headset = EntityUid.Invalid;
        RemComp<ActiveRadioComponent>(uid);
    }

    private void OnMapInit(EntityUid uid, PolyComponent component, ref MapInitEvent args)
    {
        // Init Poly's speech buffer)
        _ = FillBuffer(new Entity<PolyComponent>(uid, component));

        // Check if idiot bird is wearing a headset
        if (!_inventorySystem.TryGetSlotEntity(uid, _earSlot, out var ears))
            return;

        if (!TryComp<ActiveRadioComponent>(ears, out var radio))
            return;

        component.Headset = (EntityUid)ears;
        EnsureComp<ActiveRadioComponent>(uid).Channels = [..radio.Channels];
    }

    private void OnRadioReceive(EntityUid uid, PolyComponent component, RadioReceiveEvent args)
    {
        var message = args.Message;
        var author = args.MessageSource;
        var channel = args.Channel.ID;

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

        LearnSentence(new Entity<PolyComponent>(uid, component),
            component.LocalLearnProbability,
            "local",
            message,
            author);
    }

    // I don't think I need this anymore now that memory gets saved on component shutdown
    private void OnRoundEnd()
    {
        if (!_configManager.GetCVar(CCVars.PolyPersistantMemory))
            return;

        foreach (var component in EntityQuery<PolyComponent>())
        {
            _ = FlushMemory(component);
        }
    }

    private void LearnSentence(Entity<PolyComponent> poly,
        float probability,
        string channel,
        string sentence,
        EntityUid author)
    {
        if (_mobStateSystem.IsIncapacitated(poly.Owner))
            return; // L bozo

        if (HasComp<PolyComponent>(author))
        {
            _sawmill.Debug("Poly can't learn from another Poly or itself!");
            return;
        }

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
            return;
        }

        Guid? authorGuid = null; // This is terrible and I hate it
        if (TryComp<MindContainerComponent>(author, out var mind))
        {
            if (TryComp<MindComponent>(mind.Mind, out var mindComp))
                authorGuid = mindComp.UserId;
        }

        // Is 255 a lot? Idk, it might be too much
        if (sentence.Length > 255)
                sentence = sentence[..255];

        poly.Comp.Memory.Add((channel, sentence, authorGuid));
        _sawmill.Info($"Poly learned new sentence \"{sentence}\" from {author}");
        _adminLogManager.Add(LogType.PolyLearned, LogImpact.Medium, $"{ToPrettyString(poly.Owner)} learned new sentence \"{sentence}\" from {ToPrettyString(author):player}");
    }

    private async Task FillBuffer(Entity<PolyComponent> poly)
    {
        if (!_configManager.GetCVar(CCVars.PolyPersistantMemory))
            return;

        var sentences = await _db.PopulatePolyBuffer(); // This might not return anything if the database is empty, so TODO: add check for this
        poly.Comp.SpeechBuffer = [..sentences.Select(s => (s.Channel, s.Phrase))];
    }

    private void SayRandomSentence(Entity<PolyComponent> poly)
    {
        var (channel, sentence, _) = PickRandomSentence(poly);

        // If Poly is unable to transmit on the requested channel, the message just gets thrown into local.
        if (_prototypeManager.TryIndex<RadioChannelPrototype>(channel, out var prototype) &&
            TryComp<ActiveRadioComponent>(poly.Owner, out var transmitter) &&
            transmitter.Channels.Contains(prototype.ID) &&
            poly.Comp.Headset.Valid)
        {
            // TODO: Figure out a way to sync the transformed speech from chatSystem with radioSystem
            _chatSystem.TrySendInGameICMessage(poly.Owner, sentence, InGameICChatType.Whisper, ChatTransmitRange.Normal);
            _radioSystem.SendRadioMessage(poly.Owner, _chatSystem.TransformSpeech(poly.Owner, sentence), prototype, poly.Owner); // Poly the radio
            return;
        }

        _chatSystem.TrySendInGameICMessage(poly.Owner, sentence, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private (string channel, string sentence, Guid? author) PickRandomSentence(Entity<PolyComponent> poly)
    {
        // Poly can use a few sources for sentences, currently they are
        // 1. Memory
        // 2. Speech buffer (Randomly pulled from the database)
        // 3. Random law generation

        if (_robustRandom.Prob(0.5f) && poly.Comp.Memory.Count > 0)
        {
            var index = _robustRandom.Next(poly.Comp.Memory.Count);
            var sentence = poly.Comp.Memory.ElementAt(index);
            return sentence;
        }

        if (_robustRandom.Prob(0.5f) || poly.Comp.SavedMemory)
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

        // TODO: Replace with localized random string, probably from SS13 Poly
        var law = _ionStormSystem.GenerateLaw().ToLower();
        var cleanedLaw = _chatSystem.SanitizeInGameICMessage(poly.Owner, law, out _);
        var channel = _robustRandom.Pick(new[] {"Local", "Engineering", "Common"});

        return (channel, cleanedLaw, null);
    }

    private async Task FlushMemory(PolyComponent poly)
    {
        if (!_configManager.GetCVar(CCVars.PolyPersistantMemory))
            return;

        if (poly.Memory.Count == 0 || poly.SavedMemory)
            return;

        foreach (var (channel, sentence, guid) in poly.Memory)
        {
            await _db.InsertPolyMemory(channel, sentence, guid);
        }
        _sawmill.Info($"{poly.Memory.Count} memories saved");
        poly.Memory.Clear();
        poly.SavedMemory = true;
    }
}
