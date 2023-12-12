using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client.Audio;

public sealed class AudioUIController : UIController
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    public override void Initialize()
    {
        base.Initialize();

        /*
         * This exists to load UI sounds outside of the game sim.
         */

        // No unsub coz never shuts down until program exit.
        _configManager.OnValueChanged(CCVars.UIClickSound, SetClickSound, true);
        _configManager.OnValueChanged(CCVars.UIHoverSound, SetHoverSound, true);
    }

    private void SetClickSound(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var resource = _cache.GetResource<AudioResource>(value);
            var source =
                _audioManager.CreateAudioSource(resource);

            if (source != null)
            {
                source.Gain = 0.25f;
                source.Global = true;
            }

            UIManager.SetClickSound(source);
        }
        else
        {
            UIManager.SetClickSound(null);
        }
    }

    private void SetHoverSound(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var hoverResource = _cache.GetResource<AudioResource>(value);
            var hoverSource =
                _audioManager.CreateAudioSource(hoverResource);

            if (hoverSource != null)
            {
                hoverSource.Gain = 0.05f;
                hoverSource.Global = true;
            }

            UIManager.SetHoverSound(hoverSource);
        }
        else
        {
            UIManager.SetHoverSound(null);
        }
    }
}
