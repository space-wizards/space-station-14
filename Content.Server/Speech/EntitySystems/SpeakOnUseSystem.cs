using Content.Server.Chat.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.Actions.Events;


namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// As soon as the chat refactor moves to Shared
/// the logic here can move to the shared <see cref="SharedSpeakOnUseSystem"/>
/// </summary>
public sealed class SpeakOnUseSystem : SharedSpeakOnUseSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnUseComponent, ActionPerformedEvent>(OnSpeakOnUse);
    }

    private void OnSpeakOnUse(Entity<SpeakOnUseComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;

        // If we can't speak, we can't speak
        if (!HasComp<SpeechComponent>(user) || HasComp<MutedComponent>(user))
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.Sentence))
            return;

        _chat.TrySendInGameICMessage(user, Loc.GetString(ent.Comp.Sentence), InGameICChatType.Speak, false);
    }
}
