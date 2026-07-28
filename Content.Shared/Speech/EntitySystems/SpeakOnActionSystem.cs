using Content.Shared.Speech.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Chat;
using Content.Shared.Speech.Muting;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeakOnActionSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;

        // If we can't speak, we can't speak.
        if (!HasComp<SpeechComponent>(user) || HasComp<MutedComponent>(user))
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.Sentence))
            return;

        _chat.TrySendInGameICMessage(user, Loc.GetString(ent.Comp.Sentence), InGameICChatType.Speak, false);
    }
}
