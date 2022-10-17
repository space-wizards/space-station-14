using Content.Shared.Radiation.Components;
using Robust.Shared.Audio;

namespace Content.Client.Radiation.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    [DataField("showControl")]
    public bool ShowControl = true;

    [DataField("sounds")]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };


    public bool UiUpdateNeeded;
    public IPlayingAudioStream? Stream;
}
