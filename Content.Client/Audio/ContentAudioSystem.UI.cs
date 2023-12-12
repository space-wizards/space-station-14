using Robust.Client.ResourceManagement;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private void InitializeUI()
    {
        var resource = _cache.GetResource<AudioResource>("/Audio/UI/click2.ogg");
        var source =
            _audioManager.CreateAudioSource(resource);

        if (source != null)
        {
            source.Gain = 0.25f;
            source.Global = true;
        }

        _uiManager.SetClickSound(source);
    }
}
