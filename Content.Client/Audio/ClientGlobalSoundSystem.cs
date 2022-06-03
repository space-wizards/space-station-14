using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Audio;

public sealed class ClientGlobalSoundSystem : SharedGlobalSoundSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    // Admin music
    private bool _adminAudioEnabled = true;
    private List<IPlayingAudioStream?> _adminAudio = new(1);

    // Event sounds (e.g. nuke timer)
    private bool _eventAudioEnabled = true;
    private Dictionary<StationEventMusicType, IPlayingAudioStream?> _eventAudio = new(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AdminSoundEvent>(PlayAdminSound);
        _cfg.OnValueChanged(CCVars.AdminSoundsEnabled, ToggleAdminSound, true);

        SubscribeNetworkEvent<StationEventMusicEvent>(PlayStationEventMusic);
        SubscribeNetworkEvent<StopStationEventMusic>(StopStationEventMusic);
        _cfg.OnValueChanged(CCVars.EventMusicEnabled, ToggleStationEventMusic, true);

        SubscribeNetworkEvent<GameGlobalSoundEvent>(PlayGameSound);
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

        var stream = SoundSystem.Play(Filter.Local(), soundEvent.Filename, soundEvent.AudioParams);
        _adminAudio.Add(stream);
    }

    private void PlayStationEventMusic(StationEventMusicEvent soundEvent)
    {
        // Either the cvar is disabled or it's already playing
        if(!_eventAudioEnabled || _eventAudio.ContainsKey(soundEvent.Type)) return;

        var stream = SoundSystem.Play(Filter.Local(), soundEvent.Filename, soundEvent.AudioParams);
        _eventAudio.Add(soundEvent.Type, stream);
    }

    private void PlayGameSound(GameGlobalSoundEvent soundEvent)
    {
        var stream = SoundSystem.Play(Filter.Local(), soundEvent.Filename, soundEvent.AudioParams);
    }

    private void StopStationEventMusic(StopStationEventMusic soundEvent)
    {
        if (_eventAudio.ContainsKey(soundEvent.Type))
        {
            _eventAudio[soundEvent.Type]?.Stop();
            _eventAudio.Remove(soundEvent.Type);
        }
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

    private void ToggleStationEventMusic(bool enabled)
    {
        _eventAudioEnabled = enabled;
        if (_eventAudioEnabled) return;
        foreach (var stream in _eventAudio)
        {
            stream.Value?.Stop();
        }
        _eventAudio.Clear();
    }
}
