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
    public Dictionary<GeigerDangerLevel, SoundCollectionSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundCollectionSpecifier("GeigerLow")},
        {GeigerDangerLevel.Med, new SoundCollectionSpecifier("GeigerMed")},
        {GeigerDangerLevel.High, new SoundCollectionSpecifier("GeigerHigh")},
        {GeigerDangerLevel.Extreme, new SoundCollectionSpecifier("GeigerExt")}
    };


    public bool UiUpdateNeeded;
    public IPlayingAudioStream? Stream;
}
