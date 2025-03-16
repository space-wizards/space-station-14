using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class PlayAudioContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "PlayAudio";

    public override string Name => SupportedNodeName;

    [Dependency] private readonly IEntityManager _entManager = default!;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node)
    {
        IoCManager.InjectDependencies(this);

        var audioSystem = _entManager.System<AudioSystem>();
        var volume = 1f;
        if (node.Attributes.TryGetValue("volume", out var volumeParam) && volumeParam.LongValue.HasValue)
            volume = volumeParam.LongValue.Value;

        if (node.Value.StringValue != null)
        {
            var soundSpecifier = audioSystem.ResolveSound(new SoundPathSpecifier(node.Value.StringValue, null));
            _entManager.System<AudioSystem>().PlayGlobal(soundSpecifier, Filter.Local(), false, AudioParams.Default.WithVolume(volume));
        }

        return [];
    }

    public static bool TryCreate(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        processor = new PlayAudioContentTagProcessor();
        return true;
    }
}
