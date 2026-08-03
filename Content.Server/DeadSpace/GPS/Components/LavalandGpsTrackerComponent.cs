// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.GPS.Components;

[RegisterComponent]
public sealed partial class LavalandGpsTrackerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float DetectionRange = 100f;

    [DataField]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Items/locator_beep.ogg");

    [DataField]
    public float ScanInterval = 1f;

    [DataField]
    public float MinBeepInterval = 0.25f;

    [DataField]
    public float MaxBeepInterval = 1.5f;

    public TimeSpan NextUpdateTime;
}
