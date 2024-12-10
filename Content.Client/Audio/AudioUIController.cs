using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.Audio.Mixers;
using Robust.Client.Audio.Sources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Audio;

public sealed class AudioUIController : UIController
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IAudioMixersManager _audioMixersManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private const float ClickGain = 0.25f;
    private const float HoverGain = 0.05f;
    private static readonly ProtoId<AudioMixerPrototype> Mixer = "Interface";

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
            var resource = GetSoundOrFallback(value, CCVars.UIClickSound.DefaultValue);
            var source =
                _audioManager.CreateAudioSource(resource);

            if (source != null)
            {
                var mixableSource = new MixableAudioSource(source)
                {
                    Gain = ClickGain,
                    Global = true,
                };
                mixableSource.SetMixer(_audioMixersManager?.GetMixer(Mixer));
                source = mixableSource;
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
            var hoverResource = GetSoundOrFallback(value, CCVars.UIHoverSound.DefaultValue);
            var hoverSource =
                _audioManager.CreateAudioSource(hoverResource);

            if (hoverSource != null)
            {
                var mixableSource = new MixableAudioSource(hoverSource)
                {
                    Gain = HoverGain,
                    Global = true,
                };
                mixableSource.SetMixer(_audioMixersManager?.GetMixer(Mixer));
                hoverSource = mixableSource;
            }

            UIManager.SetHoverSound(hoverSource);
        }
        else
        {
            UIManager.SetHoverSound(null);
        }
    }

    private AudioResource GetSoundOrFallback(string path, string fallback)
    {
        if (!_cache.TryGetResource(path, out AudioResource? resource))
            return _cache.GetResource<AudioResource>(fallback);

        return resource;
    }
}
