using Content.Shared.Speech.Components;
using Content.Shared.Actions.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeakOnActionSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;

        // If we can't speak, we can't speak.
        if (!HasComp<SpeechComponent>(user) || !_actionBlocker.CanSpeak(user))
            return;

        if (ent.Comp.SpeakChance != null && !_random.Prob(ent.Comp.SpeakChance.Value))
            return;

        var randomSentence = GetDialogue(ent.Comp.Sentences);

        if (!string.IsNullOrWhiteSpace(randomSentence))
            _chat.TrySendInGameICMessage(user, Loc.GetString(randomSentence), InGameICChatType.Speak, false);
        else if (!string.IsNullOrWhiteSpace(ent.Comp.Sentence))
            _chat.TrySendInGameICMessage(user, Loc.GetString(ent.Comp.Sentence), InGameICChatType.Speak, false);
    }

    private string? GetDialogue(ProtoId<LocalizedDatasetPrototype>? dialogue)
    {
        if (!_proto.TryIndex(dialogue, out var proto))
            return null;

        return _random.Pick(proto.Values);
    }
}
