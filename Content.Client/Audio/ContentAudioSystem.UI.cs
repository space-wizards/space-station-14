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
            source.Gain = 0.15f;
            source.Global = true;
        }

        _uiManager.SetClickSound(source);

        var hoverResource = _cache.GetResource<AudioResource>("/Audio/UI/diamond.ogg");
        var hoverSource =
            _audioManager.CreateAudioSource(hoverResource);

        if (hoverSource != null)
        {
            hoverSource.Gain = 0.05f;
            hoverSource.Global = true;
        }

        _uiManager.SetHoverSound(hoverSource);
    }
}
