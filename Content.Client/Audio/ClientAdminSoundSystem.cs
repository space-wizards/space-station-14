using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Audio;

public sealed class ClientAdminSoundSystem : SharedAdminSoundSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    
    private bool _adminAudioEnabled = true;
    private List<IPlayingAudioStream?> _adminAudio = new(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AdminSoundEvent>(PlayAdminSound);
        _cfg.OnValueChanged(CCVars.AdminSoundsEnabled, ToggleAdminSound, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        foreach (var stream in _adminAudio)
        {
            stream?.Stop();
        }
        _adminAudio.Clear();
    }

    private void PlayAdminSound(AdminSoundEvent soundEvent)
    {
        if(!_adminAudioEnabled) return;

        var stream = SoundSystem.Play(soundEvent.Filename, Filter.Local(), soundEvent.AudioParams);
        _adminAudio.Add(stream);
    }

    private void ToggleAdminSound(bool enabled)
    {
        _adminAudioEnabled = enabled;
        if (_adminAudioEnabled) return;
        foreach (var stream in _adminAudio)
        {
            stream?.Stop();
        }
        _adminAudio.Clear();
    }
}
