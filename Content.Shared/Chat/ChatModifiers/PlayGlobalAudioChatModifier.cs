using Content.Shared.Chat.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the message in a [PlayAudio="path"] tag based on the entity speaking.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class PlayGlobalAudioChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (!chatMessageContext.TryGet<string>(DefaultChannelParameters.GlobalAudioPath, out var audioPath))
            return message;

        var volume = chatMessageContext.TryGet<float>(DefaultChannelParameters.GlobalAudioVolume, out var audioVolume)
            ? audioVolume
            : 1f;
        var nodeParams = new Dictionary<string, MarkupParameter>
        {
            ["volume"] = new MarkupParameter((long)volume)
        };
        var audioNode = new MarkupNode("PlayAudio", new MarkupParameter(audioPath), nodeParams, false);
        message.PushTag(audioNode, true);

        return message;
    }
}
