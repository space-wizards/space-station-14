using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SoftspokenAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SoftspokenAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<SoftspokenAccentComponent, TransformSpeechTypeEvent>(OnTransformSpeech);
    }

    /// <summary>
    /// Make the user end their sentences with an ellipses...
    /// to accentuate how quiet they are...
    /// kind of like they're mumbling...
    /// </summary>
    private void OnAccent(Entity<SoftspokenAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        if (char.IsLetter(message[^1]))
            message += "...";

        args.Message = message;
    }

    /// <summary>
    /// force the user to whisper if they have this component
    /// </summary>
    private void OnTransformSpeech(Entity<SoftspokenAccentComponent> entity, ref TransformSpeechTypeEvent args)
    {
        args.ChatType = InGameICChatType.Whisper;
    }
}
