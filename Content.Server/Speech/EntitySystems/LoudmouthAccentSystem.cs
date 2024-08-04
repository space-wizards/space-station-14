using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class LoudmouthAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoudmouthAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<LoudmouthAccentComponent, TransformSpeechTypeEvent>(OnTransformSpeech);
    }

    /// <summary>
    /// MAKE THE USER SPEAK IN ALL UPPERCASE, THEY ARE REALLY LOUD!
    /// </summary>
    private void OnAccent(Entity<LoudmouthAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        if (char.IsLetter(message[^1]))
            message += "!";

        args.Message = message.ToUpper();
    }

    /// <summary>
    /// FORCE THE USER TO SPEAK IF THEY HAVE THIS COMPONENT!
    /// </summary>
    private void OnTransformSpeech(Entity<LoudmouthAccentComponent> entity, ref TransformSpeechTypeEvent args)
    {
        args.ChatType = InGameICChatType.Speak;
    }
}
