using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;

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
            if (_timing.CurTime < component.NextUtterance)
                continue;
            var humanoid = HasComp<HumanoidAppearanceComponent>(uid);
            var shouldEcho = TryComp<MindContainerComponent>(uid, out var mind) && (humanoid ? (mind.HasMind) : (!mind.HasMind));
                // only souled humanoids or non-humanoids without souls, echo (stops a bug with cosmic cult shunting)
            if (!shouldEcho) continue;
            if (component.NextUtterance != null)
            {
                var speech = EnsureComp<SpeechComponent>(uid);
                var oldVerb = speech.SpeechVerb;
                speech.SpeechVerb = "Echo"; // Add a special speech verb override for echoing (NOTE: gets replaced with "Exclaims" anyway if the message echoed ends in a "!", arguably intended)
                _chat.TrySendInGameICMessage(
                    uid,
                    _random.Pick(component.LearnedPhrases),
                    InGameICChatType.Speak,
                    hideChat: !humanoid, // Only humanoids can be heard in chat with randomly generated messages (imp edit to shut up poly)
                    hideLog: true, // TODO: Don't spam admin logs either.
                                   // If a parrot learns something inappropriate, admins can search for
                                   // the player that said the inappropriate thing.
                    checkRadioPrefix: false);
                speech.SpeechVerb = oldVerb; //Revert speech verb
                //because changing the speechverb in this "hacky" way does not use code intended for use by voicemasks, hopefully admins don't get constant logs from this.
                //hopefully
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
