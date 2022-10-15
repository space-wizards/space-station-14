using Content.Shared.Radiation.Components;
using Robust.Shared.Audio;

namespace Content.Client.Radiation.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    [DataField("showControl")]
    public bool ShowControl = true;

    public bool UiUpdateNeeded;
    public IPlayingAudioStream? Stream;
}
