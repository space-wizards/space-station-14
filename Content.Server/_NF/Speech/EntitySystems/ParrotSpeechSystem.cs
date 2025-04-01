using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Server._NF.Speech.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Content.Shared.Chat.TypingIndicator; //imp
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Server.GameObjects; //imp

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class ParrotSpeechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotSpeechComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotSpeechComponent, ListenAttemptEvent>(CanListen);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ParrotSpeechComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.LearnedPhrases.Count == 0)
                // This parrot has not learned any phrases, so can't say anything interesting.
                continue;

            if (component.RequiresMind && !TryComp<MindContainerComponent>(uid, out var mind) | mind != null && !mind!.HasMind) // imp
                continue;

            if (component.NextUtterance != null) // imp - changed this whole deal
            {
                if (component.FakeTypingIndicator)
                {
                    CheckOrSetDelay(uid, component);
                }
                else if (_timing.CurTime > component.NextUtterance)
                {
                    SendMessage(uid, component);
                }
            }

            if (_timing.CurTime < component.NextUtterance) // imp - moved this below the actual speech code. otherwise the typing indicator length would effectively be (FakeTypingLength + NextUtterance)
                continue;

            component.NextUtterance = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumWait, component.MaximumWait));
        }
    }

    /// <summary>
    /// Sets a new typing delay time if there isn't one. If there is, checks it against CurTime, sends the message once the delay is up, and resets. 
    /// </summary>
    private void CheckOrSetDelay(EntityUid uid, ParrotSpeechComponent component)
    {
        if (component.NextFakeTypingSend == null && _timing.CurTime > component.NextUtterance)
        {
            component.NextMessage = _random.Pick(component.LearnedPhrases);
            component.NextFakeTypingSend = _timing.CurTime + TimeSpan.FromSeconds(0.1 * component.NextMessage.Length);
            _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, true);
        }
        else if (_timing.CurTime > component.NextFakeTypingSend)
        {
            SendMessage(uid, component);
            _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, false);

            // and reset.
            component.NextFakeTypingSend = null;
        }
    }

    private void SendMessage(EntityUid uid, ParrotSpeechComponent component) // imp. moved this out of Update() and to its own method to reduce repitition repitition.
    {
        _chat.TrySendInGameICMessage(
        uid,
            component.NextMessage ?? _random.Pick(component.LearnedPhrases),
            InGameICChatType.Speak,
            hideChat: component.HideMessagesInChat, // Don't spam the chat with randomly generated messages(... unless its funny (imp change))
            hideLog: true, // TODO: Don't spam admin logs either. If a parrot learns something inappropriate, admins can search for the player that said the inappropriate thing.
            checkRadioPrefix: false);
    }

    private void OnListen(EntityUid uid, ParrotSpeechComponent component, ref ListenEvent args)
    {
        if (_random.Prob(component.LearnChance))
        {
            // Very approximate word splitting. But that's okay: parrots aren't smart enough to
            // split words correctly.
            var words = args.Message.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            // Prefer longer phrases
            var phraseLength = 1 + (int)(Math.Sqrt(_random.NextDouble()) * component.MaximumPhraseLength);

            var startIndex = _random.Next(0, Math.Max(0, words.Length - phraseLength + 1));

            var phrase = string.Join(" ", words.Skip(startIndex).Take(phraseLength)).ToLower();

            while (component.LearnedPhrases.Count >= component.MaximumPhraseCount)
            {
                _random.PickAndTake(component.LearnedPhrases);
            }

            component.LearnedPhrases.Add(phrase);
        }
    }

    private void CanListen(EntityUid uid, ParrotSpeechComponent component, ref ListenAttemptEvent args)
    {
        if (_whitelistSystem.IsBlacklistPass(component.Blacklist, args.Source))
            args.Cancel();
    }
}
