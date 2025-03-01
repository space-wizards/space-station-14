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
        var audioSystem = _entManager.System<AudioSystem>();
        var volume = 1f;
        if (node.Attributes.TryGetValue("volume", out var volumeParam) && volumeParam.LongValue.HasValue)
        {
            volume = volumeParam.LongValue.Value;
        }
        if (node.Value.StringValue != null)
            _entManager.System<AudioSystem>().PlayGlobal(audioSystem.ResolveSound(new SoundPathSpecifier(node.Value.StringValue, null)), Filter.Local(), false, AudioParams.Default.WithVolume(volume));

        return null;
    }
}
