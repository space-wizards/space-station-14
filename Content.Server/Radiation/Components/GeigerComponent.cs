using Content.Shared.Radiation.Components;
using Robust.Shared.Audio;

namespace Content.Server.Radiation.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    [DataField("sounds")]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };

    [DataField("attachedToSuit")]
    public bool AttachedToSuit;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    public IPlayingAudioStream? Stream;
}
