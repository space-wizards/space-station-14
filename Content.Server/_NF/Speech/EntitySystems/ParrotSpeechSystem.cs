using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Speech.EntitySystems;

public sealed class ParrotSpeechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

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
            if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
                // Pause parrot speech when someone is controlling the parrot.
                continue;
            if (_timing.CurTime < component.NextUtterance)
                continue;

            if (component.NextUtterance != null)
            {
                _chat.TrySendInGameICMessage(
                    uid,
                    _random.Pick(component.LearnedPhrases),
                    InGameICChatType.Speak,
                    hideChat: true, // Don't spam the chat with randomly generated messages
                    hideLog: true, // TODO: Don't spam admin logs either.
                                   // If a parrot learns something inappropriate, admins can search for
                                   // the player that said the inappropriate thing.
                    checkRadioPrefix: false);
            }

            component.NextUtterance = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumWait, component.MaximumWait));
        }
    }

    private void OnListen(EntityUid uid, ParrotSpeechComponent component, ref ListenEvent args)
    {
        if (_random.Prob(component.LearnChance))
        {
            // Very approximate word splitting. But that's okay: parrots aren't smart enough to
            // split words correctly.
            var words = args.Message.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            // Prefer longer phrases
            var phraseLength = 1 + (int) (Math.Sqrt(_random.NextDouble()) * component.MaximumPhraseLength);

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
