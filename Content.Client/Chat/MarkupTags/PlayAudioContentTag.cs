using Content.Shared.Chat.ContentMarkupTags;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class PlayAudioContentTag : IContentMarkupTag
{
    public string Name => "PlayAudio";

    [Dependency] private readonly IEntityManager _entManager = default!;

    public List<MarkupNode>? OpenerProcessing(MarkupNode node, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        var volume = 1f;
        if (node.Attributes.TryGetValue("volume", out var volumeParam) && volumeParam.LongValue.HasValue)
        {
            volume = volumeParam.LongValue.Value;
        }
        _entManager.System<AudioSystem>().PlayGlobal(node.Value.StringValue, Filter.Local(), false, AudioParams.Default.WithVolume(volume));

        return null;
    }
}
