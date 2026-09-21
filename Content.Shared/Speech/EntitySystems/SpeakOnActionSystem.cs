using Content.Shared.Speech.Components;
using Content.Shared.Actions.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeakOnActionSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // If we can't speak, we can't speak.
        if (!HasComp<SpeechComponent>(user) || !_actionBlocker.CanSpeak(user))
            return;

        if (ent.Comp.SpeakChance != null && !random.Prob(ent.Comp.SpeakChance.Value))
            return;

        var randomSentence = GetDialogue(ent.Comp.Sentences, random);

        if (!string.IsNullOrWhiteSpace(randomSentence))
            _chat.TrySendInGameICMessage(user, Loc.GetString(randomSentence), InGameICChatType.Speak, false);
        else if (!string.IsNullOrWhiteSpace(ent.Comp.Sentence))
            _chat.TrySendInGameICMessage(user, Loc.GetString(ent.Comp.Sentence), InGameICChatType.Speak, false);
    }

    private string? GetDialogue(ProtoId<LocalizedDatasetPrototype>? dialogue, IRobustRandom random)
    {
        if (!_proto.TryIndex(dialogue, out var proto))
            return null;

        return random.Pick(proto.Values);
    }
}
