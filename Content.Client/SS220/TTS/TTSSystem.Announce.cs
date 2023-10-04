// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.AnnounceTTS;
using Robust.Shared.Utility;

namespace Content.Client.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    internal float VolumeAnnounce = 0f;
    internal EntityUid AnnouncementUid = EntityUid.Invalid;

    private void InitializeAnnounces()
    {
        _cfg.OnValueChanged(CCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        SubscribeNetworkEvent<AnnounceTTSEvent>(OnAnnounceTTSPlay);
    }

    private void ShutdownAnnounces()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
    }

    private void OnAnnounceTTSPlay(AnnounceTTSEvent ev)
    {
        // Early creation of entities can lead to crashes, so we postpone it as much as possible
        if (AnnouncementUid == EntityUid.Invalid)
            AnnouncementUid = Spawn(null);

        var finalParams = ev.AnnouncementParams.WithVolume(VolumeAnnounce);

        // Play announcement sound
        var announcementSoundPath = new ResPath(ev.AnnouncementSound);
        PlaySoundQueued(AnnouncementUid, announcementSoundPath, finalParams, true);

        // Play announcement itself
        PlayTTSBytes(ev.Data, AnnouncementUid, finalParams, true);
    }

    private void OnTtsAnnounceVolumeChanged(float volume)
    {
        VolumeAnnounce = volume;
    }
}
