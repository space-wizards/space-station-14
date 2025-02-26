using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the message in a [PlayAudio="path"] tag based on the entity speaking.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class PlayGlobalAudioChatModifier : ChatModifier
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (channelParameters.TryGetValue(DefaultChannelParameters.GlobalAudioPath, out var audioPath))
        {
            var volume = channelParameters.TryGetValue(DefaultChannelParameters.GlobalAudioVolume, out var audioVolume)
                ? (float)audioVolume
                : 1f;
            message.PushTag(new MarkupNode("PlayAudio", new MarkupParameter((string)audioPath), new Dictionary<string, MarkupParameter>() { { "volume", new MarkupParameter((long)volume) } }, false), true);
        }
    }
}
